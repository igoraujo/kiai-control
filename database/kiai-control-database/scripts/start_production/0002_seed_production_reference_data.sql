-- Dados globais do sistema: perfis, permissões e concessões padrão.
-- Idempotente: pode ser executado novamente sem duplicar registros.
-- Não cria organização ou usuário/senha padrão. O dono global é provisionado
-- pela configuração segura da API; ele cadastra as organizações pelo painel.

begin;

insert into roles (id, code, name, description)
values
    (gen_random_uuid(), 'client', 'Aluno', 'Acesso do aluno aos recursos próprios.'),
    (gen_random_uuid(), 'teacher', 'Professor', 'Acesso operacional do professor.'),
    (gen_random_uuid(), 'admin', 'Administrador', 'Administração da organização, exceto concessão de permissões.'),
    (gen_random_uuid(), 'financial', 'Financeiro', 'Acesso à gestão financeira e relatórios.'),
    (gen_random_uuid(), 'reception', 'Recepção', 'Acesso operacional de recepção.'),
    (gen_random_uuid(), 'owner', 'Proprietário', 'Acesso completo à organização.')
on conflict (code) do update
set name = excluded.name,
    description = excluded.description;

insert into permissions (id, code, name, description)
values
    (gen_random_uuid(), 'access.manage', 'Gerenciar acessos', 'Consultar perfis e gerenciar atribuições de perfis às pessoas.'),
    (gen_random_uuid(), 'access.permissions.manage', 'Gerenciar permissões', 'Conceder ou revogar permissões dos perfis.'),
    (gen_random_uuid(), 'audit.read', 'Consultar auditoria', 'Consultar eventos de auditoria da organização.'),
    (gen_random_uuid(), 'attendance.check-in', 'Fazer check-in', 'Permitir que o aluno faça check-in nas próprias aulas.'),
    (gen_random_uuid(), 'attendance.manage', 'Gerenciar presenças', 'Registrar, revisar e validar presenças.'),
    (gen_random_uuid(), 'billing.manage', 'Gerenciar financeiro', 'Gerenciar planos, faturas e pagamentos.'),
    (gen_random_uuid(), 'branches.manage', 'Gerenciar unidades', 'Gerenciar unidades da organização.'),
    (gen_random_uuid(), 'clients.manage', 'Gerenciar alunos', 'Gerenciar cadastros de alunos e vínculos com modalidades.'),
    (gen_random_uuid(), 'fitness.manage', 'Gerenciar condicionamento físico', 'Gerenciar metas e evolução física.'),
    (gen_random_uuid(), 'graduation.manage', 'Gerenciar graduações', 'Gerenciar sistemas de graduação e evolução dos alunos.'),
    (gen_random_uuid(), 'lessons.manage', 'Gerenciar aulas', 'Gerenciar aulas e chamadas.'),
    (gen_random_uuid(), 'modalities.manage', 'Gerenciar modalidades', 'Gerenciar modalidades da organização.'),
    (gen_random_uuid(), 'notifications.read', 'Consultar notificações', 'Consultar e marcar como lidas as notificações do usuário atual.'),
    (gen_random_uuid(), 'notifications.manage', 'Gerenciar notificações', 'Criar e gerenciar notificações da organização.'),
    (gen_random_uuid(), 'organizations.manage', 'Gerenciar organização', 'Gerenciar configurações e cadastro da organização.'),
    (gen_random_uuid(), 'ranking.manage', 'Gerenciar ranking', 'Gerenciar ciclos, classificações e medalhas.'),
    (gen_random_uuid(), 'reports.read', 'Consultar relatórios', 'Consultar painéis e relatórios da organização.'),
    (gen_random_uuid(), 'teachers.manage', 'Gerenciar professores', 'Gerenciar professores e suas atribuições.')
on conflict (code) do update
set name = excluded.name,
    description = excluded.description;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where role.code = 'owner'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where role.code = 'admin'
  and permission.code <> 'access.permissions.manage'
on conflict (role_id, permission_id) do nothing;

insert into role_permissions (role_id, permission_id)
select role.id, permission.id
from roles role
cross join permissions permission
where (role.code = 'client' and permission.code in ('attendance.check-in', 'notifications.read'))
   or (role.code = 'teacher' and permission.code in (
       'attendance.manage', 'fitness.manage', 'graduation.manage', 'lessons.manage',
       'notifications.read', 'notifications.manage', 'ranking.manage'
   ))
   or (role.code = 'reception' and permission.code in (
       'attendance.manage', 'branches.manage', 'clients.manage', 'lessons.manage',
       'notifications.read', 'notifications.manage'
   ))
   or (role.code = 'financial' and permission.code in ('billing.manage', 'reports.read'))
on conflict (role_id, permission_id) do nothing;

commit;
