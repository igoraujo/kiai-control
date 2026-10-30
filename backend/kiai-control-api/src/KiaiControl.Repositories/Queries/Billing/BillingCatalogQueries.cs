namespace KiaiControl.Repositories.Queries.Billing;

public static class BillingCatalogQueries
{
    public const string InsertPlan = @"
        insert into plans (
            id, organization_id, name, description, monthly_amount, due_day, is_active, created_at
        )
        values (
            @Id, @OrganizationId, @Name, @Description, @MonthlyAmount, @DueDay, true, @CreatedAt
        )
        returning
            id,
            organization_id as OrganizationId,
            name,
            description,
            monthly_amount as MonthlyAmount,
            due_day as DueDay,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListPlans = @"
        select
            id,
            organization_id as OrganizationId,
            name,
            description,
            monthly_amount as MonthlyAmount,
            due_day as DueDay,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from plans
        where organization_id = @OrganizationId
        order by name
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string UpdatePlan = @"
        update plans
        set
            name = @Name,
            description = @Description,
            monthly_amount = @MonthlyAmount,
            due_day = @DueDay,
            is_active = @IsActive,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning
            id,
            organization_id as OrganizationId,
            name,
            description,
            monthly_amount as MonthlyAmount,
            due_day as DueDay,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string DeactivatePlan = @"
        update plans
        set is_active = false, updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @PlanId and is_active = true;
        ";

    public const string CreateSubscription = @"
        insert into client_plan_subscriptions (
            id, organization_id, client_id, plan_id, starts_at, is_active, created_at
        )
        select @Id, c.organization_id, c.id, p.id, @StartsAt, true, @CreatedAt
        from clients c
        join plans p on p.id = @PlanId
                   and p.organization_id = c.organization_id
                   and p.is_active = true
        where c.id = @ClientId
          and c.organization_id = @OrganizationId
          and c.current_status = 'active'
          and not exists (
              select 1 from client_plan_subscriptions s
              where s.organization_id = c.organization_id
                and s.client_id = c.id
                and s.is_active = true
          )
        returning
            id,
            organization_id as OrganizationId,
            client_id as ClientId,
            plan_id as PlanId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string CancelSubscription = @"
        update client_plan_subscriptions
        set is_active = false, ends_at = @EndsAt, updated_at = now()
        where organization_id = @OrganizationId
          and id = @SubscriptionId
          and is_active = true
          and @EndsAt >= starts_at
        returning
            id,
            organization_id as OrganizationId,
            client_id as ClientId,
            plan_id as PlanId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";

    public const string ListSubscriptions = @"
        select
            id,
            organization_id as OrganizationId,
            client_id as ClientId,
            plan_id as PlanId,
            starts_at as StartsAt,
            ends_at as EndsAt,
            is_active as IsActive,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from client_plan_subscriptions
        where organization_id = @OrganizationId
        order by is_active desc, starts_at desc, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GenerateInvoices = @"
        insert into invoices (
            id, organization_id, client_plan_subscription_id, client_id, due_date,
            amount, paid_amount, status, issued_at, created_at
        )
        select
            gen_random_uuid(),
            s.organization_id,
            s.id,
            s.client_id,
            @ReferenceMonth + (p.due_day - 1),
            p.monthly_amount,
            0,
            'pending',
            now(),
            now()
        from client_plan_subscriptions s
        join plans p on p.id = s.plan_id and p.organization_id = s.organization_id
        where s.organization_id = @OrganizationId
          and s.is_active = true
          and p.is_active = true
          and s.starts_at < (@ReferenceMonth + interval '1 month')::date
          and (s.ends_at is null or s.ends_at >= @ReferenceMonth)
        on conflict do nothing
        returning
            id,
            organization_id as OrganizationId,
            client_plan_subscription_id as ClientPlanSubscriptionId,
            client_id as ClientId,
            due_date as DueDate,
            amount,
            paid_amount as PaidAmount,
            status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt;
        ";
}