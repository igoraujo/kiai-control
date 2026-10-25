namespace KiaiControl.Repositories.Queries.Billing;

public static class BillingQueries
{
    public const string Insert = @"
        insert into invoices (id, organization_id, client_plan_subscription_id, client_id, due_date, amount, paid_amount, status, issued_at, created_at)
        values (@Id, @OrganizationId, @ClientPlanSubscriptionId, @ClientId, @DueDate, @Amount, 0, @Status::invoice_status, @CreatedAt, @CreatedAt)
        returning id, organization_id as OrganizationId, client_plan_subscription_id as ClientPlanSubscriptionId, client_id as ClientId, due_date as DueDate, amount, paid_amount as PaidAmount, status::text as Status, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string ListByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            client_plan_subscription_id as ClientPlanSubscriptionId,
            client_id as ClientId,
            due_date as DueDate,
            amount,
                paid_amount as PaidAmount,
                status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from invoices
        where organization_id = @OrganizationId
        order by due_date desc, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string GetById = @"
        select
            id,
            organization_id as OrganizationId,
            client_plan_subscription_id as ClientPlanSubscriptionId,
            client_id as ClientId,
            due_date as DueDate,
            amount,
                paid_amount as PaidAmount,
                status::text as Status,
            created_at as CreatedAt,
            updated_at as UpdatedAt
        from invoices
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string Update = @"
        update invoices
        set
            client_plan_subscription_id = @ClientPlanSubscriptionId,
            client_id = @ClientId,
            due_date = @DueDate,
            amount = @Amount,
            status = @Status::invoice_status,
            updated_at = @UpdatedAt
        where organization_id = @OrganizationId and id = @Id
        returning id, organization_id as OrganizationId, client_plan_subscription_id as ClientPlanSubscriptionId, client_id as ClientId, due_date as DueDate, amount, paid_amount as PaidAmount, status::text as Status, created_at as CreatedAt, updated_at as UpdatedAt;
        ";

    public const string Delete = @"
        delete from invoices
        where organization_id = @OrganizationId and id = @Id;
        ";

    public const string LockInvoiceForPayment = @"
        select
            amount as Amount,
            discount_amount as DiscountAmount,
            fine_amount as FineAmount,
            interest_amount as InterestAmount,
            paid_amount as PaidAmount,
            status::text as Status
        from invoices
        where organization_id = @OrganizationId and id = @InvoiceId
        for update;
        ";

    public const string InsertPayment = @"
        insert into payments (
            id, organization_id, invoice_id, amount, method, paid_at, external_reference, notes, created_at
        )
        values (
            @Id, @OrganizationId, @InvoiceId, @Amount, @Method::payment_method, @PaidAt, @ExternalReference, @Notes, @CreatedAt
        )
        returning
            id,
            organization_id as OrganizationId,
            invoice_id as InvoiceId,
            amount,
            method::text as Method,
            paid_at as PaidAt,
            external_reference as ExternalReference,
            notes,
            created_at as CreatedAt;
        ";

    public const string ApplyPaymentToInvoice = @"
        update invoices
        set
            paid_amount = paid_amount + @PaymentAmount,
            status = case
                when paid_amount + @PaymentAmount >= amount - discount_amount + fine_amount + interest_amount
                    then 'paid'::invoice_status
                else 'partially_paid'::invoice_status
            end,
            paid_at = case
                when paid_amount + @PaymentAmount >= amount - discount_amount + fine_amount + interest_amount
                    then @PaidAt
                else paid_at
            end,
            updated_at = now()
        where organization_id = @OrganizationId and id = @InvoiceId;
        ";

    public const string ListPaymentsByOrganization = @"
        select
            id,
            organization_id as OrganizationId,
            invoice_id as InvoiceId,
            amount,
            method::text as Method,
            paid_at as PaidAt,
            external_reference as ExternalReference,
            notes,
            created_at as CreatedAt
        from payments
        where organization_id = @OrganizationId
        order by paid_at desc, created_at desc
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string ListPaymentsByInvoice = @"
        select
            id,
            organization_id as OrganizationId,
            invoice_id as InvoiceId,
            amount,
            method::text as Method,
            paid_at as PaidAt,
            external_reference as ExternalReference,
            notes,
            created_at as CreatedAt
        from payments
        where organization_id = @OrganizationId and invoice_id = @InvoiceId
        order by paid_at desc, created_at desc;
        ";
}
