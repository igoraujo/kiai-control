namespace KiaiControl.Repositories.Queries.Billing;

public static class BillingQueries
{
    public const string Insert = """
        insert into invoices (id, organization_id, client_plan_subscription_id, client_id, due_date, amount, paid_amount, status, issued_at, created_at)
        values (@Id, @OrganizationId, @ClientPlanSubscriptionId, @ClientId, @DueDate, @Amount, 0, @Status, @CreatedAt, @CreatedAt)
        returning id, organization_id as OrganizationId, client_plan_subscription_id as ClientPlanSubscriptionId, client_id as ClientId, due_date as DueDate, amount, status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string ListByOrganization = """
        select
            id,
            organization_id as OrganizationId,
            client_plan_subscription_id as ClientPlanSubscriptionId,
            client_id as ClientId,
            due_date as DueDate,
            amount,
            status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from invoices
        where organization_id = @OrganizationId
        order by due_date desc, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        """;

    public const string GetById = """
        select
            id,
            organization_id as OrganizationId,
            client_plan_subscription_id as ClientPlanSubscriptionId,
            client_id as ClientId,
            due_date as DueDate,
            amount,
            status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from invoices
        where organization_id = @OrganizationId and id = @Id;
        """;

    public const string Update = """
        update invoices
        set
            client_plan_subscription_id = @ClientPlanSubscriptionId,
            client_id = @ClientId,
            due_date = @DueDate,
            amount = @Amount,
            status = @Status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, client_plan_subscription_id as ClientPlanSubscriptionId, client_id as ClientId, due_date as DueDate, amount, status, created_at as CreatedAt, updated_at as UpdatedAt;
        """;

    public const string Delete = """
        delete from invoices
        where organization_id = @OrganizationId and id = @Id;
        """;
}
