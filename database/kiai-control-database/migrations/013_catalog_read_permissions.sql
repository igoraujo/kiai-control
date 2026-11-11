begin;
insert into permissions(id,code,name,description) values
 (gen_random_uuid(),'clients.read','Consultar alunos','Visualizar cadastro e jornada dos alunos.'),
 (gen_random_uuid(),'teachers.read','Consultar professores','Visualizar o cadastro de professores.'),
 (gen_random_uuid(),'branches.read','Consultar unidades','Visualizar as unidades da academia.'),
 (gen_random_uuid(),'modalities.read','Consultar modalidades','Visualizar modalidades oferecidas.')
on conflict(code) do nothing;
insert into role_permissions(role_id,permission_id)
select r.id,p.id from roles r cross join permissions p
where r.code in ('owner','admin') and p.code in ('clients.read','teachers.read','branches.read','modalities.read')
on conflict do nothing;
commit;
