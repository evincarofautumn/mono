#include <mono/metadata/gc-internal.h>
#include <mono/mini/ir-emit.h>
#include <mono/mini/mini.h>

typedef struct InstrumentLoopClosure {
	MonoCompile *cfg;
	MonoBasicBlock *start;
	GSList *exit_blocks;
} InstrumentLoopClosure;

static void instrument_loop (MonoCompile *cfg, MonoBasicBlock *bb, InstrumentLoopClosure *closure);
static void instrument_exits (gpointer data, gpointer user);
static void instrument_complex_exit (MonoBasicBlock *current, MonoBasicBlock **target, InstrumentLoopClosure *closure);
static gboolean is_outside (const MonoBasicBlock *target, InstrumentLoopClosure *closure);
static MonoInst *make_region_call (MonoCompile *cfg, const void *func);
static void instrument_simple_exit (MonoCompile *cfg, MonoBasicBlock *current, MonoInst *last);

/* For each loop, this adds a mono_gc_region_enter call to the start of the loop
 * body, and a mono_gc_region_exit call to every branch to the start or outside
 * the loop.
 */
void
mono_instrument_loop_regions (MonoCompile *const cfg)
{
	InstrumentLoopClosure closure;
	closure.cfg = cfg;
	closure.exit_blocks = g_slist_alloc ();
	int i;
	size_t num_bblocks = cfg->num_bblocks;
	for (i = 0; i < num_bblocks; ++i) {
		MonoBasicBlock *const block = cfg->bblocks [i];
		if (block->loop_body_start && block->nesting) {
			closure.start = block;
			instrument_loop (cfg, block, &closure);
		}
	}
	g_slist_free (closure.exit_blocks);
}

static void
instrument_loop (MonoCompile *const cfg, MonoBasicBlock *const start, InstrumentLoopClosure *const closure)
{
	/* FIXME: only instrument start if !loop_blocks? */
	if (!start->loop_blocks)
		instrument_exits (start, closure);
	mono_bblock_insert_before_ins (start, start->code, make_region_call (cfg, mono_gc_region_enter));
	g_list_foreach (start->loop_blocks, instrument_exits, closure);
}

static void
instrument_exits (gpointer data, gpointer user)
{
	MonoBasicBlock *const current = data;
	InstrumentLoopClosure *const closure = user;
	MonoInst *last = current->last_ins;
	MonoCompile *const cfg = closure->cfg;
	cfg->cbb = current;
	if (last->opcode == OP_BR) {
		/* Unconditional branch outside. */
		if (is_outside (last->inst_target_bb, closure))
			instrument_simple_exit (cfg, current, last);
	} else if (MONO_IS_COND_BRANCH_OP (last)) {
		/* Conditional branch outside. */
		const gboolean true_is_outside = is_outside (last->inst_true_bb, closure);
		const gboolean false_is_outside = is_outside (last->inst_false_bb, closure);
		if (true_is_outside && false_is_outside)
			instrument_simple_exit (cfg, current, last);
		else if (true_is_outside)
			instrument_complex_exit (current, &last->inst_true_bb, closure);
		else if (false_is_outside)
			instrument_complex_exit (current, &last->inst_false_bb, closure);
	} else {
		/* Implicit fallthrough outside. */
		if (is_outside (current->next_bb, closure))
			mono_bblock_insert_before_ins (current, last, make_region_call (cfg, mono_gc_region_exit));
	}
}

static void
instrument_simple_exit (MonoCompile *const cfg, MonoBasicBlock *const current, MonoInst *const last)
{
	mono_bblock_insert_before_ins (current, last, make_region_call (cfg, mono_gc_region_exit));
}

static void
instrument_complex_exit (MonoBasicBlock *const current, MonoBasicBlock **const target, InstrumentLoopClosure *const closure)
{
	MonoBasicBlock *exit_block;
	MonoCompile *const cfg = closure->cfg;
	GSList *existing = g_slist_find (closure->exit_blocks, *target);
	mono_unlink_bblock (cfg, current, *target);
	if (existing) {
		exit_block = existing->data;
	} else {
		NEW_BBLOCK (cfg, exit_block);
		ADD_BBLOCK (cfg, exit_block);
		cfg->cbb = exit_block;
		mono_emit_region_call (cfg, mono_gc_region_exit, NULL);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, *target);
		closure->exit_blocks = g_slist_prepend (closure->exit_blocks, exit_block);
	}
	mono_link_bblock (cfg, current, exit_block);
	*target = exit_block;
}

static gboolean
is_outside (const MonoBasicBlock *const target, InstrumentLoopClosure *const closure)
{
	return target == closure->start || !g_list_find (closure->start->loop_blocks, target);
}

/* Inlined & specialized version of mono_emit_jit_icall(). */
static MonoInst *
make_region_call (MonoCompile *const cfg, const void *const func)
{
	MonoCallInst *call;
	MonoInst *args [1];
	MonoType *sig_ret;
	MonoJitICallInfo *const info = mono_find_jit_icall_by_addr (func);
	g_assert (info);
	g_assert (info->sig);
	MONO_INST_NEW_CALL (cfg, call, OP_VOIDCALL);
	EMIT_NEW_PCONST (cfg, args [0], NULL);
	call->args = args;
	call->signature = info->sig;
	call->rgctx_reg = FALSE;
	sig_ret = mini_get_underlying_type (cfg, info->sig->ret);
	type_to_eval_stack_type ((cfg), sig_ret, &call->inst);
	call->need_unbox_trampoline = FALSE;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;
	call->fptr = mono_icall_get_wrapper (info);
	return (MonoInst *)call;
}
