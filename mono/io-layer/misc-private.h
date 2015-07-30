/*
 * misc-private.h:  Miscellaneous internal support functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_MISC_PRIVATE_H_
#define _WAPI_MISC_PRIVATE_H_

#include <glib.h>
#include <sys/time.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

extern void _wapi_calc_timeout(struct timespec *timeout, guint32 ms);

#ifdef __cplusplus
}
#endif

#endif /* _WAPI_MISC_PRIVATE_H_ */
