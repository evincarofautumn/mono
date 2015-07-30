
noinst_LTLIBRARIES = libwapi.la

AM_CPPFLAGS = \
	$(GLIB_CFLAGS)		\
	$(LIBGC_CPPFLAGS)	\
	-DMONO_BINDIR=\""$(bindir)"\"	\
	-I$(top_srcdir) \
	$(SHARED_CFLAGS)

libwapiincludedir = $(includedir)/mono-$(API_VER)/mono/io-layer

OTHER_H = \
	access.h	\
	collection.h	\
	context.h	\
	error.h		\
	events.h	\
	handles.h	\
	io.h		\
	io-layer.h	\
	io-portability.h	\
	macros.h	\
	messages.h	\
	mutexes.h	\
	processes.h	\
	security.h	\
	semaphores.h	\
	sockets.h	\
	status.h	\
	threads.h	\
	timefuncs.h	\
	types.h		\
	uglify.h	\
	versioninfo.h	\
	wait.h		\
	wapi.h		\
	wapi-remap.h

OTHER_SRC = \
	access.h		\
	collection.cpp		\
	collection.h		\
	context.cpp		\
	context.h		\
	error.cpp			\
	error.h			\
	events.cpp		\
	events.h		\
	event-private.h		\
	handles.cpp		\
	handles.h		\
	handles-private.h	\
	io.cpp			\
	io.h			\
	io-portability.cpp	\
	io-portability.h	\
	io-private.h		\
	io-layer.h		\
	locking.cpp		\
	macros.h		\
	messages.cpp		\
	messages.h		\
	misc.cpp			\
	misc-private.h		\
	mutexes.cpp		\
	mutexes.h		\
	mutex-private.h		\
	posix.cpp			\
	processes.cpp		\
	processes.h		\
	process-private.h	\
	security.cpp		\
	security.h		\
	semaphores.cpp		\
	semaphores.h		\
	semaphore-private.h	\
	shared.cpp		\
	shared.h		\
	sockets.cpp		\
	sockets.h		\
	socket-private.h	\
	socket-wrappers.h	\
	status.h		\
	threads.h		\
	thread-private.h	\
	timefuncs.cpp		\
	timefuncs.h		\
	timefuncs-private.h	\
	types.h			\
	uglify.h		\
	versioninfo.cpp		\
	versioninfo.h		\
	wait.cpp			\
	wait.h			\
	wapi_glob.h		\
	wapi_glob.cpp		\
	wapi.h			\
	wapi-private.h		\
	wthreads.cpp


WINDOWS_H = \
	io-layer.h

WINDOWS_SRC = \
	io-layer.h		\
	io-layer-dummy.cpp

if HOST_WIN32
libwapi_la_SOURCES = $(WINDOWS_SRC) $(WINDOWS_H)
else
libwapi_la_SOURCES = $(OTHER_SRC) $(OTHER_H)
endif
if PLATFORM_DARWIN
libwapi_la_LIBADD = -lproc
endif

EXTRA_DIST =	\
	$(WINDOWS_SRC)
	$(OTHER_SRC)


