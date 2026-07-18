-- ============================================================
--  Tabela `reports` — canal de report do jogo (bug/hacker/feedback)
--  Rode isto UMA VEZ no Supabase: painel → SQL Editor → New query →
--  cole tudo → Run.
--
--  Projeto: https://zutdbgltjphsbakeeoda.supabase.co
--  O jogo insere com a chave publishable (anônima); só o admin lê.
-- ============================================================

create table if not exists public.reports (
  id            uuid primary key default gen_random_uuid(),
  created_at    timestamptz not null default now(),
  type          text not null default 'bug',   -- bug | hacker | feedback
  description   text,
  user_id       text,        -- id do jogador logado ("" se anônimo) — TEXT de
                             -- propósito: o cliente manda "" quando deslogado
  player_name   text,
  player_email  text,
  match_seed    int,
  my_player     int,
  map           text,        -- mesa | espaco | floresta
  round         int,         -- round em que o report foi criado (0 = lobby)
  game_version  text,
  full_log      text,        -- log completo da sessão
  round_log     text,        -- só o trecho do round reportado
  status        text not null default 'aberto' -- aberto | resolvido (você edita)
);

alter table public.reports enable row level security;

-- QUALQUER jogador (logado ou não) pode ENVIAR um report
drop policy if exists "anyone can insert reports" on public.reports;
create policy "anyone can insert reports"
  on public.reports for insert
  to anon, authenticated
  with check (true);

-- Só o ADMIN (seu e-mail) LÊ os reports
drop policy if exists "admin reads reports" on public.reports;
create policy "admin reads reports"
  on public.reports for select
  using (auth.jwt() ->> 'email' = 'carlos1995.dev@gmail.com');

-- Só o ADMIN muda o status (aberto -> resolvido)
drop policy if exists "admin updates reports" on public.reports;
create policy "admin updates reports"
  on public.reports for update
  using (auth.jwt() ->> 'email' = 'carlos1995.dev@gmail.com');

-- Só o ADMIN exclui reports
drop policy if exists "admin deletes reports" on public.reports;
create policy "admin deletes reports"
  on public.reports for delete
  using (auth.jwt() ->> 'email' = 'carlos1995.dev@gmail.com');

-- Índice para listar os mais recentes rápido
create index if not exists reports_created_idx on public.reports (created_at desc);
