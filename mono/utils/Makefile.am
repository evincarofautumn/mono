noinst_LTLIBRARIES = libmonoutils.la

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS)

if ENABLE_DTRACE

BUILT_SOURCES = mono-dtrace.h

mono-dtrace.h: $(top_srcdir)/data/mono.d
	$(DTRACE) $(DTRACEFLAGS) -h -s $(top_srcdir)/data/mono.d -o $@ || > $@

endif

monoutils_sources = \
	mono-md5.cpp		\
	mono-sha1.cpp		\
	mono-logger.cpp		\
	mono-logger-internal.h	\
	mono-codeman.cpp		\
	dlmalloc.h      	\
	dlmalloc.cpp      	\
	mono-counters.cpp		\
	mono-compiler.h		\
	mono-complex.h		\
	mono-dl.cpp		\
	mono-dl-windows.cpp	\
	mono-dl-darwin.cpp	\
	mono-dl-posix.cpp		\
	mono-dl.h		\
	mono-internal-hash.cpp	\
	mono-internal-hash.h	\
	mono-io-portability.cpp 	\
	mono-io-portability.h	\
	monobitset.cpp		\
	mono-filemap.cpp		\
	mono-math.cpp  		\
	mono-mmap.cpp  		\
	mono-mmap.h  		\
	mono-mmap-internal.h	\
	mono-mutex.cpp  		\
	mono-mutex.h  		\
	mono-lazy-init.h		\
	mono-networkinterfaces.cpp		\
	mono-networkinterfaces.h		\
	mono-proclib.cpp		\
	mono-proclib.h		\
	mono-publib.cpp		\
	mono-string.h		\
	mono-time.cpp  		\
	mono-time.h  		\
	strenc.h		\
	strenc.cpp		\
	mono-uri.cpp		\
	mono-poll.cpp		\
	mono-path.cpp		\
	mono-semaphore.cpp	\
	mono-semaphore.h	\
	mono-sigcontext.h	\
	mono-stdlib.cpp 		\
	mono-property-hash.h 	\
	mono-property-hash.cpp 	\
	mono-value-hash.h 	\
	mono-value-hash.cpp 	\
	freebsd-elf_common.h 	\
	freebsd-elf32.h		\
	freebsd-elf64.h		\
	freebsd-dwarf.h 	\
	dtrace.h			\
	gc_wrapper.h		\
	mono-error.cpp	\
	mono-error-internals.h	\
	monobitset.h	\
	mono-codeman.h	\
	mono-counters.h	\
	mono-digest.h	\
	mono-error.h	\
	mono-machine.h	\
	mono-math.h	\
	mono-membar.h	\
	mono-path.h	\
	mono-poll.h	\
	mono-uri.h	\
	mono-stdlib.h	\
	valgrind.h	\
	mach-support.cpp	\
	mach-support.h  \
	memcheck.h	\
	mono-context.cpp	\
	mono-context.h	\
	mono-stack-unwinding.h	\
	hazard-pointer.cpp	\
	hazard-pointer.h	\
	lock-free-queue.cpp	\
	lock-free-queue.h	\
	lock-free-alloc.cpppp	\
	lock-free-alloc.h	\
	lock-free-array-queue.cpp	\
	lock-free-array-queue.h	\
	mono-linked-list-set.cpp	\
	mono-linked-list-set.h	\
	mono-threads.cpp	\
	mono-threads-state-machine.cpp	\
	mono-threads-posix.cpp	\
	mono-threads-posix-signals.cpp	\
	mono-threads-posix-signals.h	\
	mono-threads-mach.cpp	\
	mono-threads-mach-helper.c	\
	mono-threads-windows.cpp	\
	mono-threads-linux.cpp	\
	mono-threads-freebsd.cpp	\
	mono-threads-openbsd.cpp	\
	mono-threads-android.cpp	\
	mono-threads.h	\
	mono-threads-api.h	\
	mono-threads-coop.cpp	\
	mono-threads-coop.h	\
	mono-threads-mach-abort-syscall.cpp	\
	mono-threads-posix-abort-syscall.cpp	\
	mono-threads-windows-abort-syscall.cpp	\
	mono-tls.h	\
	mono-tls.cpp	\
	linux_magic.h	\
	mono-memory-model.h	\
	atomic.h	\
	atomic.cpp	\
	mono-hwcap.h	\
	mono-hwcap.cpp	\
	bsearch.h	\
	bsearch.cpp	\
	mono-signal-handler.h	\
	mono-conc-hashtable.h	\
	mono-conc-hashtable.cpp	\
	sha1.h		\
	sha1.cpp	\
	json.h	\
	json.cpp	\
	networking.cpp	\
	networking-posix.cpp	\
	networking-fallback.cpp	\
	networking-missing.cpp	\
	networking-windows.cpp	\
	networking.h	\
	mono-rand.cpp	\
	mono-rand.h \
	memfuncs.cpp \
	memfuncs.h \
	parse.cpp \
	parse.h	\
	checked-build.cpp \
	checked-build.h

arch_sources = 

if !CROSS_COMPILE

if X86
arch_sources += mach-support-x86.cpp
endif

if AMD64
arch_sources += mach-support-amd64.cpp
endif

if ARM
arch_sources += mach-support-arm.cpp
endif

if ARM64
arch_sources += mach-support-arm64.cpp
endif

else

arch_sources += mach-support-unknown.cpp

endif

if X86
arch_sources += mono-hwcap-x86.cpp mono-hwcap-x86.h
endif

if AMD64
arch_sources += mono-hwcap-x86.cpp mono-hwcap-x86.h
endif

if ARM
arch_sources += mono-hwcap-arm.cpp mono-hwcap-arm.h
endif

if ARM64
arch_sources += mono-hwcap-arm64.cpp mono-hwcap-arm64.h
endif

if MIPS
arch_sources += mono-hwcap-mips.cpp mono-hwcap-mips.h
endif

if POWERPC
arch_sources += mono-hwcap-ppc.cpp mono-hwcap-ppc.h
endif

if POWERPC64
arch_sources += mono-hwcap-ppc.cpp mono-hwcap-ppc.h
endif

if SPARC
arch_sources += mono-hwcap-sparc.cpp mono-hwcap-sparc.h
endif

if SPARC64
arch_sources += mono-hwcap-sparc.cpp mono-hwcap-sparc.h
endif

if IA64
arch_sources += mono-hwcap-ia64.cpp mono-hwcap-ia64.h
endif

if S390X
arch_sources += mono-hwcap-s390x.cpp mono-hwcap-s390x.h
endif

libmonoutils_la_SOURCES = $(monoutils_sources) $(arch_sources)
libmonoutilsincludedir = $(includedir)/mono-$(API_VER)/mono/utils

libmonoutilsinclude_HEADERS = 	\
	mono-logger.h		\
	mono-error.h		\
	mono-publib.h		\
	mono-dl-fallback.h	\
	mono-counters.h

EXTRA_DIST = ChangeLog mono-embed.h mono-embed.cpp
