#include <mono/mini/mini.h>
#include <mono/metadata/gc-internal.h>

/* An inlined version of the process undertaken by 'mono_emit_jit_icall', but
 * without emitting the instruction.
 */
static MonoInst *
make_region_call (MonoCompile *cfg, gconstpointer func)
{
	MonoJitICallInfo *info = mono_find_jit_icall_by_addr (func);
	gconstpointer wrapper;
	MonoCallInst *call;
	MonoType *ret;
	g_assert (info);
	wrapper = mono_icall_get_wrapper (info);
	MONO_INST_NEW_CALL (cfg, call, OP_VOIDCALL);
	call->args = NULL;
	call->signature = info->sig;
	call->rgctx_reg = FALSE;
	ret = mini_replace_type (info->sig->ret);
	type_to_eval_stack_type (cfg, ret, &call->inst);
	call->need_unbox_trampoline = FALSE;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;
	call->fptr = func;
	return (MonoInst *)call;
}

void
mono_instrument_regions (MonoCompile *cfg)
{
#if 0
	MonoInst *enter_call, *exit_call;
	enter_call = make_region_call (cfg, mono_gc_region_enter);
	mono_bblock_insert_before_ins (cfg->bb_entry, cfg->bb_entry->code, enter_call);
	exit_call = make_region_call (cfg, mono_gc_region_exit);
	/* mono_bblock_add_inst (cfg->bblocks [cfg->num_bblocks - 1], exit_call); */
	mono_bblock_add_inst (cfg->bb_exit, exit_call);
	/* mono_bblock_insert_before_ins (cfg->bblocks [cfg->num_bblocks - 1], cfg->bblocks [cfg->num_bblocks - 1]->last_ins, exit_call); */
#else
	int i;
	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *head;
		GList *last;
		if (!cfg->bblocks [i]->loop_blocks)
			continue;
		head = (MonoBasicBlock *)cfg->bblocks [i]->loop_blocks->data;
		last = head->loop_blocks;
		if (!last)
			continue;
		while (last->next)
			last = last->next;
		{
			MonoInst *enter_call = make_region_call (cfg, mono_gc_region_enter);
			mono_bblock_insert_before_ins (head, head->code, enter_call);
		}
		{
			MonoInst *exit_call = make_region_call (cfg, mono_gc_region_exit);
			MonoBasicBlock *last_block = (MonoBasicBlock *)last->data;
			mono_bblock_add_inst (last_block, exit_call);
			/* mono_bblock_insert_before_ins (last_block, last_block->last_ins, exit_call); */
		}
		// printf ("Loop from BB%d to BB%d\n", head->block_num, ((MonoBasicBlock *)last->data)->block_num);
	}
#endif
}
