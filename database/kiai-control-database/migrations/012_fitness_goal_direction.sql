begin;
alter table fitness_goals add column if not exists direction varchar(12) not null default 'increase';
alter table fitness_goals add column if not exists initial_value numeric(12,2) not null default 0;
alter table fitness_goals drop constraint if exists ck_fitness_goal_direction;
alter table fitness_goals add constraint ck_fitness_goal_direction check (
    direction in ('increase','decrease') and initial_value >= 0
    and (direction <> 'decrease' or initial_value > target_value)
);
commit;
