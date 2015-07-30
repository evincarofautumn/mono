/* 
 * Copyright 2014 Xamarin Inc
 */
#ifndef __MONO_METADATA_REFLECTION_INTERBALS_H__
#define __MONO_METADATA_REFLECTION_INTERBALS_H__

#include <mono/metadata/reflection.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

#ifdef __cplusplus
extern "C" {
#endif

MonoObject*
mono_custom_attrs_get_attr_checked (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass, MonoError *error);

#ifdef __cplusplus
}
#endif

#endif
