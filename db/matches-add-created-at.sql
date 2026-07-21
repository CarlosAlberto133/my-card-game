-- Adiciona a coluna de data na tabela "matches".
--
-- Contexto (jul/2026): a tabela foi criada direto no painel do Supabase, sem
-- "created_at". O histórico do lobby ("ÚLTIMAS PARTIDAS") pede as partidas
-- ordenadas pela data — sem a coluna o PostgREST devolve 400 e o painel ficava
-- mostrando "Nenhuma partida ainda", escondendo o erro.
--
-- Rodar UMA VEZ no SQL Editor do Supabase. É seguro rodar de novo: o
-- "if not exists" ignora caso a coluna já exista.

alter table matches
  add column if not exists created_at timestamptz not null default now();

-- As partidas que já estavam salvas ficam todas com a data de agora (não há
-- como recuperar a original). Da próxima em diante a data é gravada sozinha.

-- Ordenar por data é a consulta mais frequente do histórico: o índice evita
-- varredura da tabela inteira conforme ela cresce.
create index if not exists matches_user_created_idx
  on matches (user_id, created_at desc);

-- Conferência (deve listar as partidas da conta logada, mais recentes primeiro):
--   select created_at, map, status, i_won, duration_seconds
--   from matches order by created_at desc limit 10;
