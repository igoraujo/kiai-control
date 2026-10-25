namespace KiaiControl.Repositories.Queries.Reports;

public static class ReportQueries
{
    public const string Dashboard = @"
        select
            (select count(*)::integer from clients c where c.organization_id = @OrganizationId and c.current_status = 'active') as ActiveClients,
            (select count(*)::integer from clients c where c.organization_id = @OrganizationId and c.joined_at between @From and @To) as NewClients,
            (select count(*)::integer from clients c where c.organization_id = @OrganizationId and c.current_status = 'canceled') as CanceledClients,
            (select count(*)::integer from lessons l where l.organization_id = @OrganizationId and l.status = 'completed' and l.starts_at::date between @From and @To) as CompletedLessons,
            coalesce((
                select 100.0 * count(*) filter (where a.status in ('present', 'late')) / nullif(count(a.id), 0)
                from attendances a
                join lessons l on l.id = a.lesson_id and l.organization_id = a.organization_id
                where a.organization_id = @OrganizationId and l.starts_at::date between @From and @To
            ), 0)::numeric(6,2) as AttendanceRate,
            (select coalesce(sum(i.amount - i.discount_amount + i.fine_amount + i.interest_amount), 0)
             from invoices i where i.organization_id = @OrganizationId and i.due_date between @From and @To) as RevenueExpected,
            (select coalesce(sum(p.amount), 0)
             from payments p where p.organization_id = @OrganizationId and p.paid_at::date between @From and @To) as RevenueReceived,
            (select coalesce(sum(i.amount - i.discount_amount + i.fine_amount + i.interest_amount - i.paid_amount), 0)
             from invoices i where i.organization_id = @OrganizationId and i.due_date between @From and @To
               and i.due_date < current_date and i.status in ('pending', 'partially_paid', 'overdue')) as RevenueOverdue;
        ";

    public const string Attendance = @"
        select cg.id as ClassGroupId,
               cg.name as ClassGroupName,
               count(distinct l.id)::integer as LessonsHeld,
               count(a.id) filter (where a.status in ('present', 'late'))::integer as AttendanceCount,
               count(a.id) filter (where a.status in ('absent', 'justified_absence'))::integer as AbsenceCount,
               coalesce(100.0 * count(a.id) filter (where a.status in ('present', 'late')) / nullif(count(a.id), 0), 0)::numeric(6,2) as AttendanceRate
        from class_groups cg
        left join lessons l on l.class_group_id = cg.id and l.organization_id = cg.organization_id
                           and l.status = 'completed' and l.starts_at::date between @From and @To
        left join attendances a on a.lesson_id = l.id and a.organization_id = cg.organization_id
        where cg.organization_id = @OrganizationId
          and (@ModalityId is null or cg.modality_id = @ModalityId)
        group by cg.id, cg.name
        order by cg.name;
        ";

    public const string Financial = @"
        select
            coalesce(sum(amount - discount_amount + fine_amount + interest_amount), 0) as AmountIssued,
            coalesce(sum(paid_amount), 0) as AmountPaid,
            coalesce(sum(greatest(amount - discount_amount + fine_amount + interest_amount - paid_amount, 0)), 0) as AmountOutstanding,
            coalesce(sum(greatest(amount - discount_amount + fine_amount + interest_amount - paid_amount, 0))
                filter (where due_date < current_date and status in ('pending', 'partially_paid', 'overdue')), 0) as AmountOverdue,
            count(*)::integer as InvoicesIssued,
            count(*) filter (where due_date < current_date and status in ('pending', 'partially_paid', 'overdue'))::integer as InvoicesOverdue
        from invoices
        where organization_id = @OrganizationId and due_date between @From and @To;
        ";

    public const string ClientPerformance = @"
        with lesson_totals as (
            select e.client_id,
                   count(distinct l.id) filter (where l.status = 'completed')::integer as lessons_available,
                   count(a.id) filter (where a.status in ('present', 'late'))::integer as attendance_count
            from enrollments e
            join class_groups cg on cg.id = e.class_group_id and cg.organization_id = e.organization_id
            join lessons l on l.class_group_id = cg.id and l.organization_id = e.organization_id
                           and l.starts_at::date between @From and @To
            left join attendances a on a.lesson_id = l.id and a.client_id = e.client_id and a.organization_id = e.organization_id
            where e.organization_id = @OrganizationId
              and e.enrolled_at::date <= @To and (e.ended_at is null or e.ended_at::date >= @From)
              and (@ModalityId is null or cg.modality_id = @ModalityId)
            group by e.client_id
        ), paid_totals as (
            select invoice.client_id, sum(payment.amount) as amount_paid
            from payments payment
            join invoices invoice on invoice.id = payment.invoice_id and invoice.organization_id = payment.organization_id
            where payment.organization_id = @OrganizationId and payment.paid_at::date between @From and @To
            group by invoice.client_id
        ), fitness_totals as (
            select client_id, count(*) filter (where status = 'active')::integer as active_goals
            from fitness_goals
            where organization_id = @OrganizationId
            group by client_id
        )
        select c.id as ClientId,
               person.full_name as FullName,
               coalesce(lesson_totals.attendance_count, 0) as AttendanceCount,
               coalesce(lesson_totals.lessons_available, 0) as LessonsAvailable,
               coalesce(100.0 * lesson_totals.attendance_count / nullif(lesson_totals.lessons_available, 0), 0)::numeric(6,2) as AttendanceRate,
               coalesce(paid_totals.amount_paid, 0) as AmountPaid,
               coalesce(fitness_totals.active_goals, 0) as ActiveFitnessGoals
        from clients c
        join persons person on person.id = c.person_id and person.organization_id = c.organization_id and person.deleted_at is null
        left join lesson_totals on lesson_totals.client_id = c.id
        left join paid_totals on paid_totals.client_id = c.id
        left join fitness_totals on fitness_totals.client_id = c.id
        where c.organization_id = @OrganizationId
          and (lesson_totals.client_id is not null or paid_totals.client_id is not null or fitness_totals.client_id is not null)
        order by AttendanceRate desc, person.full_name
        offset @Offset rows fetch next @PageSize rows only;
        ";

    public const string Graduation = @"
        select m.id as ModalityId,
               m.name as ModalityName,
               count(event.id)::integer as PromotionCount,
               coalesce(max(event.event_date), @From)::date as LatestPromotion
        from modalities m
        left join graduation_events event on event.modality_id = m.id
                                         and event.organization_id = m.organization_id
                                         and event.event_type = 'promotion'
                                         and event.event_date between @From and @To
        where m.organization_id = @OrganizationId
          and (@ModalityId is null or m.id = @ModalityId)
        group by m.id, m.name
        order by m.name;
        ";
}