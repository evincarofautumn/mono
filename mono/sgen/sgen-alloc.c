/*
 * sgen-alloc.c: Object allocation routines + managed allocators
 *
 * Author:
 * 	Paolo Molaro (lupus@ximian.com)
 *  Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2005-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Copyright 2011 Xamarin, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * ######################################################################
 * ########  Object allocation
 * ######################################################################
 * This section of code deals with allocating memory for objects.
 * There are several ways:
 * *) allocate large objects
 * *) allocate normal objects
 * *) fast lock-free allocation
 * *) allocation of pinned objects
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include <string.h>

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-protocol.h"
#include "mono/sgen/sgen-memory-governor.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-memory-model.h"

#define ALIGN_UP		SGEN_ALIGN_UP
#define ALLOC_ALIGN		SGEN_ALLOC_ALIGN
#define MAX_SMALL_OBJ_SIZE	SGEN_MAX_SMALL_OBJ_SIZE

#ifdef HEAVY_STATISTICS
static guint64 stat_objects_alloced = 0;
static guint64 stat_bytes_alloced = 0;
static guint64 stat_bytes_alloced_los = 0;

static guint64 stat_regions_bailed = 0;
static guint64 stat_regions_entered = 0;
static guint64 stat_regions_exited = 0;
static guint64 stat_regions_nonzero_exited = 0;
static guint64 stat_regions_stuck = 0;
static guint64 stat_regions_forgotten = 0;
static guint64 stat_regions_reset = 0;

static guint64 stat_region_bytes_cleared = 0;
static guint64 stat_region_bytes_stuck = 0;
static double stat_region_exit_size_min = +1.0 / 0.0;
static double stat_region_exit_size_nonzero_mean = 0.0;
static double stat_region_exit_size_max = -1.0 / 0.0;

static guint64 stat_region_stuck_major_to_minor = 0;
static guint64 stat_region_stuck_stack_to_region = 0;
static guint64 stat_region_stuck_old_tlab_to_new_tlab = 0;
static guint64 stat_region_stuck_old_region_to_new_region = 0;
static guint64 stat_region_stuck_old_frame_to_new_frame = 0;
static guint64 stat_region_stuck_always = 0;

static guint64 stat_region_merged_return = 0;
#endif

/*
 * Allocation is done from a Thread Local Allocation Buffer (TLAB). TLABs are allocated
 * from nursery fragments.
 * tlab_next is the pointer to the space inside the TLAB where the next object will 
 * be allocated.
 * tlab_temp_end is the pointer to the end of the temporary space reserved for
 * the allocation: it allows us to set the scan starts at reasonable intervals.
 * tlab_real_end points to the end of the TLAB.
 */

/*
 * FIXME: What is faster, a TLS variable pointing to a structure, or separate TLS 
 * variables for next+temp_end ?
 */
#ifdef HAVE_KW_THREAD
static __thread char *tlab_start;
static __thread char *tlab_next;
static __thread char *tlab_temp_end;
static __thread char *tlab_real_end;
/* Used by the managed allocator/wbarrier */
static __thread char **tlab_next_addr MONO_ATTR_USED;
/* Stack of pointers to region starts. */
static __thread char **tlab_regions_begin, **tlab_regions_end, **tlab_regions_capacity;
/* Address below which we cannot clear regions, due to escaped pointers. */
char *tlab_stuck;
#endif

#ifdef HAVE_KW_THREAD
#define TLAB_START	tlab_start
#define TLAB_NEXT	tlab_next
#define TLAB_TEMP_END	tlab_temp_end
#define TLAB_REAL_END	tlab_real_end
#define TLAB_REGIONS_BEGIN	tlab_regions_begin
#define TLAB_REGIONS_END	tlab_regions_end
#define TLAB_REGIONS_CAPACITY	tlab_regions_capacity
#define TLAB_STUCK	tlab_stuck
#else
#define TLAB_START	(__thread_info__->tlab_start)
#define TLAB_NEXT	(__thread_info__->tlab_next)
#define TLAB_TEMP_END	(__thread_info__->tlab_temp_end)
#define TLAB_REAL_END	(__thread_info__->tlab_real_end)
#define TLAB_REGIONS_BEGIN	(__thread_info__->tlab_regions_begin)
#define TLAB_REGIONS_END	(__thread_info__->tlab_regions_end)
#define TLAB_REGIONS_CAPACITY	(__thread_info__->tlab_regions_capacity)
#define TLAB_STUCK	(__thread_info__->tlab_stuck)
#endif

static GCObject*
alloc_degraded (GCVTable vtable, size_t size, gboolean for_mature)
{
	GCObject *p;

	if (!for_mature) {
		sgen_client_degraded_allocation (size);
		SGEN_ATOMIC_ADD_P (degraded_mode, size);
		sgen_ensure_free_space (size, GENERATION_OLD);
	} else {
		if (sgen_need_major_collection (size))
			sgen_perform_collection (size, GENERATION_OLD, "mature allocation failure", !for_mature);
	}


	p = major_collector.alloc_degraded (vtable, size);

	if (!for_mature)
		binary_protocol_alloc_degraded (p, vtable, size, sgen_client_get_provenance ());

	return p;
}

static void
zero_tlab_if_necessary (void *p, size_t size)
{
	if (nursery_clear_policy == CLEAR_AT_TLAB_CREATION || nursery_clear_policy == CLEAR_AT_TLAB_CREATION_DEBUG) {
		memset (p, 0, size);
	} else {
		/*
		 * This function is called for all allocations in
		 * TLABs.  TLABs originate from fragments, which are
		 * initialized to be faux arrays.  The remainder of
		 * the fragments are zeroed out at initialization for
		 * CLEAR_AT_GC, so here we just need to make sure that
		 * the array header is zeroed.  Since we don't know
		 * whether we're called for the start of a fragment or
		 * for somewhere in between, we zero in any case, just
		 * to make sure.
		 */
		sgen_client_zero_array_fill_header (p, size);
	}
}

static size_t
region_stack_size ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	return TLAB_REGIONS_END - TLAB_REGIONS_BEGIN;
}

static size_t
region_stack_capacity ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	return TLAB_REGIONS_CAPACITY - TLAB_REGIONS_BEGIN;
}

static void
region_stack_push (gpointer start)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	SGEN_ASSERT (0, TLAB_REGIONS_END <= TLAB_REGIONS_CAPACITY, "Region stack overflow");
	/* If full or not allocated then (re)allocate.
	 *
	 * FIXME: Make this allocation safe without locking.
	 */
	if (TLAB_REGIONS_END == TLAB_REGIONS_CAPACITY) {
		const size_t size = region_stack_size ();
		const size_t capacity = region_stack_capacity ();
		size_t new_capacity = capacity + capacity / 2;
		if (!new_capacity)
			new_capacity = 1024;
		TLAB_REGIONS_BEGIN = g_realloc (TLAB_REGIONS_BEGIN, new_capacity * sizeof (*TLAB_REGIONS_BEGIN));
		TLAB_REGIONS_END = TLAB_REGIONS_BEGIN + size;
		TLAB_REGIONS_CAPACITY = TLAB_REGIONS_BEGIN + new_capacity;
	}
	/* Save TLAB pointer. */
	*TLAB_REGIONS_END++ = start;
}

static void
region_stack_pop ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	SGEN_ASSERT (0, TLAB_REGIONS_END > TLAB_REGIONS_BEGIN, "Region stack underflow");
	--TLAB_REGIONS_END;
}

static gpointer
region_stack_peek ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	SGEN_ASSERT (0, TLAB_REGIONS_END > TLAB_REGIONS_BEGIN, "Region stack underflow");
	return TLAB_REGIONS_END [-1];
}

static void
region_stack_clear ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	TLAB_REGIONS_END = TLAB_REGIONS_BEGIN;
	TLAB_STUCK = NULL;
}

static gboolean
region_stack_empty ()
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	return TLAB_REGIONS_BEGIN == TLAB_REGIONS_END;
}

static gboolean
sgen_ptr_in_tlab (gpointer ptr)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif
	return (char *)ptr >= TLAB_START && (char *)ptr < TLAB_REAL_END;
}

/* Removes pointers to stuck regions from the region stack. */
static size_t
forget_stuck_regions (void)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif

	char **p = TLAB_REGIONS_BEGIN;

#ifdef HEAVY_STATISTICS
	if (!region_stack_empty () && TLAB_STUCK > TLAB_START && TLAB_STUCK <= TLAB_REAL_END)
		stat_region_bytes_stuck += TLAB_STUCK - *TLAB_REGIONS_BEGIN;
#endif

	/* Find first non-stuck region. */
	while (p != TLAB_REGIONS_END && *p <= TLAB_STUCK)
		++p;

	const size_t forgotten = p - TLAB_REGIONS_BEGIN;
	const size_t remaining = TLAB_REGIONS_END - p;
	HEAVY_STAT (stat_regions_forgotten += forgotten);

	/* Shift non-stuck regions. */
	g_memmove (TLAB_REGIONS_BEGIN, p, remaining * sizeof (*p));
	TLAB_REGIONS_END = TLAB_REGIONS_BEGIN + remaining;
	if (region_stack_empty ())
		TLAB_STUCK = NULL;

	return forgotten;
}

/* Intercepts writes of 'src' into 'dst'. If they are not in the same
 * region, then this sticks the region of 'src'. If 'dst' is 'NULL',
 * the region is always stuck.
 */
void
mono_gc_stick_region_if_necessary (gpointer src, gpointer dst)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif

	/* FIXME: Remove locking. */
	sgen_gc_lock ();

	if (!sgen_ptr_in_tlab (src))
		goto end;

	if (!TLAB_REGIONS_BEGIN || region_stack_empty ()) {
		SGEN_ASSERT (0, !TLAB_STUCK, "Region info was not cleared correctly (stuck = %p, regions_begin = %p, regions_end = %p)", TLAB_STUCK, TLAB_REGIONS_BEGIN, TLAB_REGIONS_END);
		goto end;
	}

	if (TLAB_STUCK)
		SGEN_ASSERT (0, sgen_ptr_in_tlab (TLAB_STUCK), "Region info was not cleared correctly (stuck %p is not in %p-%p)", TLAB_STUCK, TLAB_START, TLAB_REAL_END);

	gboolean major_to_minor = !sgen_ptr_in_nursery (dst);
	gboolean old_tlab_to_new_tlab = !sgen_ptr_in_tlab (dst);
	/* FIXME: This is too conservative; it should check whether region_of(dst) < region_of(src). */
	gboolean old_region_to_new_region = dst < src;
	/* FIXME: This is WAY too conservative; it should check whether the destination is in a higher stack frame. */
	gboolean old_frame_to_new_frame = sgen_ptr_on_stack (dst);
	gboolean always_stick = !dst;

#ifdef HEAVY_STATISTICS
	if (always_stick)
		++stat_region_stuck_always;
	else if (major_to_minor) {
		if (dst > (void *)&__thread_info__ && dst < (void *)__thread_info__->client_info.stack_end)
			++stat_region_stuck_stack_to_region;
		else
			++stat_region_stuck_major_to_minor;
	} else if (old_tlab_to_new_tlab)
		++stat_region_stuck_old_tlab_to_new_tlab;
	else if (old_region_to_new_region)
		++stat_region_stuck_old_region_to_new_region;
	else if (old_frame_to_new_frame)
		++stat_region_stuck_old_frame_to_new_frame;
#endif

	if (major_to_minor || old_tlab_to_new_tlab || old_region_to_new_region || old_frame_to_new_frame || always_stick) {
		TLAB_STUCK = MAX (TLAB_STUCK, TLAB_NEXT);
		forget_stuck_regions ();
		HEAVY_STAT (++stat_regions_stuck);
	}

end:
	sgen_gc_unlock ();
}

/* Bail out of all regions. Always safe, but slow. */
void
mono_gc_region_bail (void)
{
	/* FIXME: Remove locking. */
	sgen_gc_lock ();
	HEAVY_STAT (++stat_regions_bailed);
	region_stack_clear ();
	sgen_gc_unlock ();
}

/* Enter a new region by pushing the current TLAB pointer onto the region stack. */
void
mono_gc_region_enter (void)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif

	/* FIXME: Remove locking. */
	sgen_gc_lock ();

	HEAVY_STAT (++stat_regions_entered);

	/* A GC has happened, and we're not in a TLAB, so there's nothing to do. */
	if (!TLAB_NEXT) {
		SGEN_ASSERT (0, !TLAB_STUCK, "The TLAB info has been reset incorrectly");
		goto end;
	}

	region_stack_push (TLAB_NEXT);

end:
	sgen_gc_unlock ();
}

void describe_region (void *start, size_t size);

/* Exit the current region, zeroing its memory and resetting the TLAB pointer.  */
void
mono_gc_region_exit (gpointer ret)
{
// #ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = mono_thread_info_current ();
// #endif

	sgen_gc_lock ();

	HEAVY_STAT (++stat_regions_exited);

	/* FIXME: At this point, there might (should?) be no regions to forget. */
	forget_stuck_regions ();

	/* The region we would've exited has already been forgotten, so there's
	 * nothing to do.
	 */
	if (region_stack_empty ()) {
		SGEN_ASSERT (0, !TLAB_STUCK, "The TLAB info has been reset incorrectly");
		goto end;
	}

	char *region = region_stack_peek ();

	SGEN_ASSERT (0, sgen_ptr_in_tlab (region) || region == TLAB_REAL_END, "Region pointers should always be in the current TLAB");
	if (TLAB_STUCK)
		SGEN_ASSERT (0, sgen_ptr_in_tlab (TLAB_STUCK), "The TLAB info has been reset incorrectly");
	SGEN_ASSERT (0, region >= TLAB_STUCK, "Stuck regions should not be accessible");

	/* A GC has happened, and we're not in a TLAB, so there's nothing to do. */
	if (!TLAB_NEXT) {
		SGEN_ASSERT (0, !TLAB_STUCK, "The TLAB info has been reset incorrectly");
		goto end;
	}

	size_t region_size = TLAB_NEXT - region;

	HEAVY_STAT (stat_region_exit_size_min = region_size < stat_region_exit_size_min ? region_size : stat_region_exit_size_min);
	HEAVY_STAT (stat_region_exit_size_max = region_size > stat_region_exit_size_max ? region_size : stat_region_exit_size_max);

	/* If we're returning a reference, we don't need to stick: we can simply
	 * merge with the previous region.
	 *
	 * FIXME: Only merge if the reference is from the current region.
	 * FIXME: The test of TLAB_STUCK may be unnecessary.
	 */
	if (ret && !TLAB_STUCK) {
		HEAVY_STAT (++stat_region_merged_return);
		region_stack_pop ();
		goto end;
	}

	/* Actually clear objects in the region. */
	if (region_size) {
		memset (region, 0, region_size);

#ifdef HEAVY_STATISTICS
		stat_region_bytes_cleared += region_size;
		++stat_regions_nonzero_exited;
		const size_t n = stat_regions_nonzero_exited;
		stat_region_exit_size_nonzero_mean = n == 1 ? region_size
			: ((n - 1) * stat_region_exit_size_nonzero_mean + region_size) / n;
#endif
	}

	/* Reset TLAB pointer. */
	TLAB_NEXT = region;
	region_stack_pop ();
	if (region_stack_empty ())
		TLAB_STUCK = NULL;

end:
	sgen_gc_unlock ();
}

/*
 * Provide a variant that takes just the vtable for small fixed-size objects.
 * The aligned size is already computed and stored in vt->gc_descr.
 * Note: every SGEN_SCAN_START_SIZE or so we are given the chance to do some special
 * processing. We can keep track of where objects start, for example,
 * so when we scan the thread stacks for pinned objects, we can start
 * a search for the pinned object in SGEN_SCAN_START_SIZE chunks.
 */
GCObject*
sgen_alloc_obj_nolock (GCVTable vtable, size_t size)
{
	/* FIXME: handle OOM */
	void **p;
	char *new_next;
	size_t real_size = size;
	TLAB_ACCESS_INIT;
	
	CANARIFY_SIZE(size);

	HEAVY_STAT (++stat_objects_alloced);
	if (real_size <= SGEN_MAX_SMALL_OBJ_SIZE)
		HEAVY_STAT (stat_bytes_alloced += size);
	else
		HEAVY_STAT (stat_bytes_alloced_los += size);

	size = ALIGN_UP (size);

	SGEN_ASSERT (6, sgen_vtable_get_descriptor (vtable), "VTable without descriptor");

	if (G_UNLIKELY (has_per_allocation_action)) {
		static int alloc_count;
		int current_alloc = InterlockedIncrement (&alloc_count);

		if (collect_before_allocs) {
			if (((current_alloc % collect_before_allocs) == 0) && nursery_section) {
				sgen_perform_collection (0, GENERATION_NURSERY, "collect-before-alloc-triggered", TRUE);
				if (!degraded_mode && sgen_can_alloc_size (size) && real_size <= SGEN_MAX_SMALL_OBJ_SIZE) {
					// FIXME:
					g_assert_not_reached ();
				}
			}
		} else if (verify_before_allocs) {
			if ((current_alloc % verify_before_allocs) == 0)
				sgen_check_whole_heap_stw ();
		}
	}

	/*
	 * We must already have the lock here instead of after the
	 * fast path because we might be interrupted in the fast path
	 * (after confirming that new_next < TLAB_TEMP_END) by the GC,
	 * and we'll end up allocating an object in a fragment which
	 * no longer belongs to us.
	 *
	 * The managed allocator does not do this, but it's treated
	 * specially by the world-stopping code.
	 */

	if (real_size > SGEN_MAX_SMALL_OBJ_SIZE) {
		p = (void **)sgen_los_alloc_large_inner (vtable, ALIGN_UP (real_size));
	} else {
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;
		TLAB_NEXT = new_next;

		if (G_LIKELY (new_next < TLAB_TEMP_END)) {
			/* Fast path */

			/* 
			 * FIXME: We might need a memory barrier here so the change to tlab_next is 
			 * visible before the vtable store.
			 */

			CANARIFY_ALLOC(p,real_size);
			SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
			binary_protocol_alloc (p , vtable, size, sgen_client_get_provenance ());
			g_assert (*p == NULL);
			mono_atomic_store_seq (p, vtable);

			return (GCObject*)p;
		}

		/* Slow path */

		/* there are two cases: the object is too big or we run out of space in the TLAB */
		/* we also reach here when the thread does its first allocation after a minor 
		 * collection, since the tlab_ variables are initialized to NULL.
		 * there can be another case (from ORP), if we cooperate with the runtime a bit:
		 * objects that need finalizers can have the high bit set in their size
		 * so the above check fails and we can readily add the object to the queue.
		 * This avoids taking again the GC lock when registering, but this is moot when
		 * doing thread-local allocation, so it may not be a good idea.
		 */
		if (TLAB_NEXT >= TLAB_REAL_END) {
			int available_in_tlab;
			/* 
			 * Run out of space in the TLAB. When this happens, some amount of space
			 * remains in the TLAB, but not enough to satisfy the current allocation
			 * request. Currently, we retire the TLAB in all cases, later we could
			 * keep it if the remaining space is above a treshold, and satisfy the
			 * allocation directly from the nursery.
			 */
			TLAB_NEXT -= size;
			/* when running in degraded mode, we continue allocing that way
			 * for a while, to decrease the number of useless nursery collections.
			 */
			if (degraded_mode && degraded_mode < DEFAULT_NURSERY_SIZE)
				return alloc_degraded (vtable, size, FALSE);

			available_in_tlab = (int)(TLAB_REAL_END - TLAB_NEXT);//We'll never have tlabs > 2Gb
			if (size > tlab_size || available_in_tlab > SGEN_MAX_NURSERY_WASTE) {
				/* Allocate directly from the nursery */
				p = (void **)sgen_nursery_alloc (size);
				if (!p) {
					/*
					 * We couldn't allocate from the nursery, so we try
					 * collecting.  Even after the collection, we might
					 * still not have enough memory to allocate the
					 * object.  The reason will most likely be that we've
					 * run out of memory, but there is the theoretical
					 * possibility that other threads might have consumed
					 * the freed up memory ahead of us.
					 *
					 * What we do in this case is allocate degraded, i.e.,
					 * from the major heap.
					 *
					 * Ideally we'd like to detect the case of other
					 * threads allocating ahead of us and loop (if we
					 * always loop we will loop endlessly in the case of
					 * OOM).
					 */
					sgen_ensure_free_space (real_size, GENERATION_NURSERY);
					if (!degraded_mode)
						p = (void **)sgen_nursery_alloc (size);
				}
				if (!p)
					return alloc_degraded (vtable, size, FALSE);

				zero_tlab_if_necessary (p, size);
			} else {
				size_t alloc_size = 0;
				if (TLAB_START)
					SGEN_LOG (3, "Retire TLAB: %p-%p [%ld]", TLAB_START, TLAB_REAL_END, (long)(TLAB_REAL_END - TLAB_NEXT - size));
				sgen_nursery_retire_region (p, available_in_tlab);

				p = (void **)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
				if (!p) {
					/* See comment above in similar case. */
					sgen_ensure_free_space (tlab_size, GENERATION_NURSERY);
					if (!degraded_mode)
						p = (void **)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
				}
				if (!p)
					return alloc_degraded (vtable, size, FALSE);

				/* Allocate a new TLAB from the current nursery fragment */
				TLAB_START = (char*)p;
				TLAB_NEXT = TLAB_START;
				TLAB_REAL_END = TLAB_START + alloc_size;
				TLAB_TEMP_END = TLAB_START + MIN (SGEN_SCAN_START_SIZE, alloc_size);
				HEAVY_STAT (stat_regions_reset += region_stack_size ());
				region_stack_clear ();

				zero_tlab_if_necessary (TLAB_START, alloc_size);

				/* Allocate from the TLAB */
				p = (void **)TLAB_NEXT;
				TLAB_NEXT += size;
				sgen_set_nursery_scan_start ((char*)p);
			}
		} else {
			/* Reached tlab_temp_end */

			/* record the scan start so we can find pinned objects more easily */
			sgen_set_nursery_scan_start ((char*)p);
			/* we just bump tlab_temp_end as well */
			TLAB_TEMP_END = MIN (TLAB_REAL_END, TLAB_NEXT + SGEN_SCAN_START_SIZE);
			SGEN_LOG (5, "Expanding local alloc: %p-%p", TLAB_NEXT, TLAB_TEMP_END);
		}
		CANARIFY_ALLOC(p,real_size);
	}

	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
		binary_protocol_alloc (p, vtable, size, sgen_client_get_provenance ());
		mono_atomic_store_seq (p, vtable);
	}

	if (mono_class_has_finalizer (vtable->klass))
		mono_gc_stick_region_if_necessary (p, NULL);

	return (GCObject*)p;
}

GCObject*
sgen_try_alloc_obj_nolock (GCVTable vtable, size_t size)
{
	void **p;
	char *new_next;
	size_t real_size = size;
	TLAB_ACCESS_INIT;

	CANARIFY_SIZE(size);

	size = ALIGN_UP (size);
	SGEN_ASSERT (9, real_size >= SGEN_CLIENT_MINIMUM_OBJECT_SIZE, "Object too small");

	SGEN_ASSERT (6, sgen_vtable_get_descriptor (vtable), "VTable without descriptor");

	if (real_size > SGEN_MAX_SMALL_OBJ_SIZE)
		return NULL;

	if (G_UNLIKELY (size > tlab_size)) {
		/* Allocate directly from the nursery */
		p = (void **)sgen_nursery_alloc (size);
		if (!p)
			return NULL;
		sgen_set_nursery_scan_start ((char*)p);

		/*FIXME we should use weak memory ops here. Should help specially on x86. */
		zero_tlab_if_necessary (p, size);
	} else {
		int available_in_tlab;
		char *real_end;
		/* tlab_next and tlab_temp_end are TLS vars so accessing them might be expensive */

		p = (void**)TLAB_NEXT;
		/* FIXME: handle overflow */
		new_next = (char*)p + size;

		real_end = TLAB_REAL_END;
		available_in_tlab = (int)(real_end - (char*)p);//We'll never have tlabs > 2Gb

		if (G_LIKELY (new_next < real_end)) {
			TLAB_NEXT = new_next;

			/* Second case, we overflowed temp end */
			if (G_UNLIKELY (new_next >= TLAB_TEMP_END)) {
				sgen_set_nursery_scan_start (new_next);
				/* we just bump tlab_temp_end as well */
				TLAB_TEMP_END = MIN (TLAB_REAL_END, TLAB_NEXT + SGEN_SCAN_START_SIZE);
				SGEN_LOG (5, "Expanding local alloc: %p-%p", TLAB_NEXT, TLAB_TEMP_END);
			}

		} else if (available_in_tlab > SGEN_MAX_NURSERY_WASTE) {
			/* Allocate directly from the nursery */
			p = (void **)sgen_nursery_alloc (size);
			if (!p)
				return NULL;

			zero_tlab_if_necessary (p, size);
		} else {
			size_t alloc_size = 0;

			sgen_nursery_retire_region (p, available_in_tlab);
			new_next = (char *)sgen_nursery_alloc_range (tlab_size, size, &alloc_size);
			p = (void**)new_next;
			if (!p)
				return NULL;

			HEAVY_STAT (stat_regions_reset += region_stack_size ());
			TLAB_START = (char*)new_next;
			TLAB_NEXT = new_next + size;
			TLAB_REAL_END = new_next + alloc_size;
			TLAB_TEMP_END = new_next + MIN (SGEN_SCAN_START_SIZE, alloc_size);
			region_stack_clear ();

			sgen_set_nursery_scan_start ((char*)p);

			zero_tlab_if_necessary (new_next, alloc_size);
		}
	}

	if (mono_class_has_finalizer (vtable->klass))
		mono_gc_stick_region_if_necessary (p, NULL);

	HEAVY_STAT (++stat_objects_alloced);
	HEAVY_STAT (stat_bytes_alloced += size);

	CANARIFY_ALLOC(p,real_size);
	SGEN_LOG (6, "Allocated object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
	binary_protocol_alloc (p, vtable, size, sgen_client_get_provenance ());
	g_assert (*p == NULL); /* FIXME disable this in non debug builds */

	mono_atomic_store_seq (p, vtable);

	return (GCObject*)p;
}

GCObject*
sgen_alloc_obj (GCVTable vtable, size_t size)
{
	GCObject *res;
	TLAB_ACCESS_INIT;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;

	if (G_UNLIKELY (has_per_allocation_action)) {
		static int alloc_count;
		int current_alloc = InterlockedIncrement (&alloc_count);

		if (verify_before_allocs) {
			if ((current_alloc % verify_before_allocs) == 0)
				sgen_check_whole_heap_stw ();
		}
		if (collect_before_allocs) {
			if (((current_alloc % collect_before_allocs) == 0) && nursery_section) {
				LOCK_GC;
				sgen_perform_collection (0, GENERATION_NURSERY, "collect-before-alloc-triggered", TRUE);
				UNLOCK_GC;
			}
		}
	}

	ENTER_CRITICAL_REGION;
	res = sgen_try_alloc_obj_nolock (vtable, size);
	if (res) {
		EXIT_CRITICAL_REGION;
		return res;
	}
	EXIT_CRITICAL_REGION;

	LOCK_GC;
	res = sgen_alloc_obj_nolock (vtable, size);
	UNLOCK_GC;
	return res;
}

/*
 * To be used for interned strings and possibly MonoThread, reflection handles.
 * We may want to explicitly free these objects.
 */
GCObject*
sgen_alloc_obj_pinned (GCVTable vtable, size_t size)
{
	GCObject *p;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;
	size = ALIGN_UP (size);

	LOCK_GC;

	if (size > SGEN_MAX_SMALL_OBJ_SIZE) {
		/* large objects are always pinned anyway */
		p = (GCObject *)sgen_los_alloc_large_inner (vtable, size);
	} else {
		SGEN_ASSERT (9, sgen_client_vtable_is_inited (vtable), "class %s:%s is not initialized", sgen_client_vtable_get_namespace (vtable), sgen_client_vtable_get_name (vtable));
		p = major_collector.alloc_small_pinned_obj (vtable, size, SGEN_VTABLE_HAS_REFERENCES (vtable));
	}
	if (G_LIKELY (p)) {
		SGEN_LOG (6, "Allocated pinned object %p, vtable: %p (%s), size: %zd", p, vtable, sgen_client_vtable_get_name (vtable), size);
		binary_protocol_alloc_pinned (p, vtable, size, sgen_client_get_provenance ());
	}
	UNLOCK_GC;
	return p;
}

GCObject*
sgen_alloc_obj_mature (GCVTable vtable, size_t size)
{
	GCObject *res;

	if (!SGEN_CAN_ALIGN_UP (size))
		return NULL;
	size = ALIGN_UP (size);

	LOCK_GC;
	res = alloc_degraded (vtable, size, TRUE);
	UNLOCK_GC;

	return res;
}

void
sgen_init_tlab_info (SgenThreadInfo* info)
{
#ifndef HAVE_KW_THREAD
	SgenThreadInfo *__thread_info__ = info;
#endif

	info->tlab_start_addr = &TLAB_START;
	info->tlab_next_addr = &TLAB_NEXT;
	info->tlab_temp_end_addr = &TLAB_TEMP_END;
	info->tlab_real_end_addr = &TLAB_REAL_END;
	info->tlab_regions_end = NULL;
	info->tlab_regions_begin = NULL;
	info->tlab_regions_capacity = NULL;

#ifdef HAVE_KW_THREAD
	tlab_next_addr = &tlab_next;
#endif
}

/*
 * Clear the thread local TLAB variables for all threads.
 */
void
sgen_clear_tlabs (void)
{
	FOREACH_THREAD (info) {
		/* A new TLAB will be allocated when the thread does its first allocation */
		*info->tlab_start_addr = NULL;
		*info->tlab_next_addr = NULL;
		*info->tlab_temp_end_addr = NULL;
		*info->tlab_real_end_addr = NULL;
		info->tlab_regions_end = info->tlab_regions_begin;
		info->tlab_stuck = NULL;
	} FOREACH_THREAD_END
}

void
sgen_init_allocator (void)
{
#if defined(HAVE_KW_THREAD) && !defined(SGEN_WITHOUT_MONO)
	int tlab_next_addr_offset = -1;
	int tlab_temp_end_offset = -1;


	MONO_THREAD_VAR_OFFSET (tlab_next_addr, tlab_next_addr_offset);
	MONO_THREAD_VAR_OFFSET (tlab_temp_end, tlab_temp_end_offset);

	mono_tls_key_set_offset (TLS_KEY_SGEN_TLAB_NEXT_ADDR, tlab_next_addr_offset);
	mono_tls_key_set_offset (TLS_KEY_SGEN_TLAB_TEMP_END, tlab_temp_end_offset);
#endif

#ifdef HEAVY_STATISTICS
	mono_counters_register ("# objects allocated", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_objects_alloced);
	mono_counters_register ("bytes allocated", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_bytes_alloced);
	mono_counters_register ("bytes allocated in LOS", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_bytes_alloced_los);

	mono_counters_register ("regions bailed", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_regions_bailed);
	mono_counters_register ("regions entered", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_regions_entered);
	mono_counters_register ("regions forgotten", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_regions_forgotten);
	mono_counters_register ("regions exited", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_regions_exited);
	mono_counters_register ("regions reset", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_regions_reset);
	mono_counters_register ("region bytes cleared", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_bytes_cleared);
	mono_counters_register ("region bytes stuck", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_bytes_stuck);
	mono_counters_register ("region exit size nonzero mean", MONO_COUNTER_GC | MONO_COUNTER_DOUBLE, &stat_region_exit_size_nonzero_mean);
	mono_counters_register ("region exit size min", MONO_COUNTER_GC | MONO_COUNTER_DOUBLE, &stat_region_exit_size_min);
	mono_counters_register ("region exit size max", MONO_COUNTER_GC | MONO_COUNTER_DOUBLE, &stat_region_exit_size_max);

	mono_counters_register ("regions stuck major->minor", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_major_to_minor);
	mono_counters_register ("regions stuck stack->region", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_stack_to_region);
	mono_counters_register ("regions stuck old->new tlab", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_old_tlab_to_new_tlab);
	mono_counters_register ("regions stuck old->new region", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_old_region_to_new_region);
	mono_counters_register ("regions stuck old->new frame", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_old_frame_to_new_frame);
	mono_counters_register ("regions stuck always", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_stuck_always);

	mono_counters_register ("regions merged return", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_region_merged_return);
#endif
}

#endif /*HAVE_SGEN_GC*/
