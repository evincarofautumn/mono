AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS)

if SUPPORT_SGEN
if DISABLE_EXECUTABLES
shared_libraries = libmonosgen.la
else
if SHARED_MONO
shared_libraries = libmonosgen.la
endif
endif
libraries = $(shared_libraries) libmonosgen-static.la
endif

if DISABLE_EXECUTABLES
noinst_LTLIBRARIES = $(shared_libraries)
else
noinst_LTLIBRARIES = $(libraries)
endif

monosgen_sources = \
	gc-internal-agnostic.h \
	sgen-alloc.cpp \
	sgen-archdep.h \
	sgen-cardtable.cpp \
	sgen-cardtable.h \
	sgen-client.h \
	sgen-conf.h \
	sgen-copy-object.h \
	sgen-debug.cpp \
	sgen-descriptor.cpp \
	sgen-descriptor.h \
	sgen-fin-weak-hash.cpp \
	sgen-gc.cpp \
	sgen-gc.h \
	sgen-gchandles.cpp \
	sgen-gray.cpp \
	sgen-gray.h \
	sgen-hash-table.cpp \
	sgen-hash-table.h \
	sgen-internal.cpp \
	sgen-layout-stats.cpp \
	sgen-layout-stats.h \
	sgen-los.cpp \
	sgen-major-copy-object.h \
	sgen-marksweep-drain-gray-stack.h \
	sgen-marksweep-scan-object-concurrent.h \
	sgen-marksweep.cpp \
	sgen-memory-governor.cpp \
	sgen-memory-governor.h \
	sgen-minor-copy-object.h \
	sgen-minor-scan-object.h \
	sgen-nursery-allocator.cpp \
	sgen-pinning-stats.cpp \
	sgen-pinning.cpp \
	sgen-pinning.h \
	sgen-pointer-queue.cpp \
	sgen-pointer-queue.h \
	sgen-protocol-def.h \
	sgen-protocol.cpp \
	sgen-protocol.h \
	sgen-qsort.cpp \
	sgen-qsort.h \
	sgen-scan-object.h \
	sgen-simple-nursery.cpp \
	sgen-split-nursery.cpp \
	sgen-tagged-pointer.h \
	sgen-thread-pool.cpp \
	sgen-thread-pool.h \
	sgen-workers.cpp \
	sgen-workers.h

libmonosgen_la_SOURCES = $(monosgen_sources)
libmonosgen_la_CFLAGS = $(SGEN_DEFINES)

libmonosgen_static_la_SOURCES = $(libmonosgen_la_SOURCES)
libmonosgen_static_la_CFLAGS = $(SGEN_DEFINES)
libmonosgen_static_la_LDFLAGS = -static
