-- ============================================================
--  MIGRAÇÃO v4.4: coluna `effect_triggers` na card_stats
--  (conta quantas vezes o EFEITO de cada carta disparou — responde
--  "quais efeitos são pouco usados" com dados de verdade)
--
--  Rode UMA VEZ no Supabase: painel → SQL Editor → New query → Run.
--  Também é uma boa hora para LIMPAR a telemetria antiga (o dano
--  pré-4.3 era subcontado e as cartas mudaram muito na 4.4):
-- ============================================================

alter table public.card_stats
  add column if not exists effect_triggers int default 0;

-- OPCIONAL (recomendado): zerar os dados antigos após distribuir a build 4.4
-- delete from public.card_stats;
