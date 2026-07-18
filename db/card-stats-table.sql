-- ============================================================
--  Tabela `card_stats` — telemetria de BALANCEAMENTO por carta
--  Rode isto UMA VEZ no Supabase: painel → SQL Editor → New query →
--  cole tudo → Run.
--
--  Projeto: https://zutdbgltjphsbakeeoda.supabase.co
--  O jogo insere com a chave publishable (anônima); só o admin lê.
--  Uma linha por (carta, dono) por partida DECIDIDA (finalizada).
-- ============================================================

create table if not exists public.card_stats (
  id             uuid primary key default gen_random_uuid(),
  created_at     timestamptz not null default now(),
  game_version   text,
  map            text,          -- mesa | espaco | floresta
  match_seed     int,           -- seed da partida (conta partidas distintas)
  card_key       text not null, -- classe|tier|atk|esc|hp (identidade única)
  card_name      text,
  card_class     text,          -- Tank | Arqueiro | Mago | Healer
  tier           int,
  owner_player   int,           -- 1 ou 2
  won            boolean,       -- o dono venceu a partida?
  bought         int default 0, -- vezes comprada na loja
  played         int default 0, -- vezes colocada em campo (da mão)
  kills          int default 0,
  deaths         int default 0,
  damage_dealt   int default 0,
  damage_taken   int default 0, -- "quanto tancou"
  healing_done   int default 0,
  gold_generated int default 0,
  debuffs_applied int default 0 -- congelamentos + stuns aplicados
);

alter table public.card_stats enable row level security;

-- QUALQUER cliente do jogo (anônimo) pode INSERIR telemetria
drop policy if exists "anyone can insert card_stats" on public.card_stats;
create policy "anyone can insert card_stats"
  on public.card_stats for insert
  to anon, authenticated
  with check (true);

-- Só o ADMIN (seu e-mail) LÊ a telemetria
drop policy if exists "admin reads card_stats" on public.card_stats;
create policy "admin reads card_stats"
  on public.card_stats for select
  using (auth.jwt() ->> 'email' = 'carlos1995.dev@gmail.com');

-- Só o ADMIN limpa a telemetria (ex.: zerar após uma grande mudança de balance)
drop policy if exists "admin deletes card_stats" on public.card_stats;
create policy "admin deletes card_stats"
  on public.card_stats for delete
  using (auth.jwt() ->> 'email' = 'carlos1995.dev@gmail.com');

-- Índices para os gráficos (filtrar por versão, agrupar por carta/classe)
create index if not exists card_stats_version_idx on public.card_stats (game_version);
create index if not exists card_stats_key_idx on public.card_stats (card_key);
create index if not exists card_stats_class_idx on public.card_stats (card_class);
create index if not exists card_stats_created_idx on public.card_stats (created_at desc);
