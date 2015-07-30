/*
 * access.h:  Access control definitions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_ACCESS_H_
#define _WAPI_ACCESS_H_

#include <glib.h>

#include <mono/io-layer/wapi.h>

#ifdef __cplusplus
extern "C" {
#endif

#define SYNCHRONIZE			0x00100000
#define STANDARD_RIGHTS_REQUIRED	0x000f0000

#ifdef __cplusplus
}
#endif

#endif /* _WAPI_ACCESS_H_ */
