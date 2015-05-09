if SUPPORT_SGEN
sgen_dirs = sgen
endif

if CROSS_COMPILING
SUBDIRS = arch utils io-layer cil metadata $(sgen_dirs) mini dis profiler
else
if INSTALL_MONOTOUCH
SUBDIRS = utils io-layer metadata arch $(sgen_dirs) mini profiler

monotouch-do-build:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="monotouch-do-build" ;; \
		xmini ) target="monotouch-do-build" ;; \
		* ) target="all" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;

monotouch-do-clean:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="monotouch-do-clean" ;; \
		xmini ) target="monotouch-do-clean" ;; \
		* ) target="clean" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;
else

mono_subdirs = arch utils io-layer cil metadata $(sgen_dirs) mini dis tests unit-tests benchmark profiler

# all: $(mono_subdirs)
# 
# $(mono_subdirs)::
# 	(cd utils && $(MAKE) $(AM_MAKEFLAGS) $2)

SUBDIR_PREFIX = subdir

define SUBDIR_RULE
.PHONY: $(SUBDIR_PREFIX)-$2-$1

$(SUBDIR_PREFIX)-$2-$1:
	@echo "Making $2 in $1"; \
	(cd $1 && $(MAKE) $(AM_MAKEFLAGS) $2)
endef

define SUBDIR_ALL
$(addprefix $(SUBDIR_PREFIX)-all-,$1)
endef

all-local: $(call SUBDIR_ALL,$(mono_subdirs))

$(foreach SUBDIR,$(mono_subdirs),$(eval $(call SUBDIR_RULE,$(SUBDIR),all)))

$(call SUBDIR_ALL,mini): $(call SUBDIR_ALL,metadata utils)

$(call SUBDIR_ALL,mini): $(call SUBDIR_ALL,arch dis io-layer metadata sgen utils)

$(call SUBDIR_ALL,profiler): $(call SUBDIR_ALL,mini)

$(call SUBDIR_ALL,tests): $(call SUBDIR_ALL,io-layer metadata sgen utils)

$(call SUBDIR_ALL,unit-tests): $(call SUBDIR_ALL,io-layer metadata sgen utils)

$(call SUBDIR_ALL,dis): $(call SUBDIR_ALL,io-layer metadata sgen utils)

# arch: metadata
# dis: cil metadata utils
# io-layer: metadata utils
# metadata: cil io-layer sgen utils
# mini: arch cil io-layer metadata utils
# profiler: metadata utils
# sgen: metadata utils
# tests: metadata
# unit-tests: metadata utils
# utils: arch io-layer metadata mini sgen

# $(call SUBDIR_ALL,monodis): $(call SUBDIR_ALL,sgen utils)
# $(call SUBDIR_ALL,tests): $(call SUBDIR_ALL,utils sgen metadata mini)
# $(call SUBDIR_ALL,unit-tests): $(call SUBDIR_ALL,utils sgen metadata mini)
# $(call SUBDIR_ALL,metadata): $(call SUBDIR_ALL,utils mini)
# $(call SUBDIR_ALL,profiler): $(call SUBDIR_ALL,sgen metadata)
# $(call SUBDIR_ALL,sgen): $(call SUBDIR_ALL,metadata)

clean-local: $(addprefix $(SUBDIR_PREFIX)-clean-,$(mono_subdirs))

$(foreach SUBDIR,$(mono_subdirs),$(eval $(call SUBDIR_RULE,$(SUBDIR),clean)))

endif
endif
DIST_SUBDIRS = arch utils io-layer cil metadata $(sgen_dirs) mini dis tests unit-tests benchmark profiler
