namespace KiaiControl.Repositories.Queries.Auth;

public static class AuthQueries
{
    public const string GetUserByEmail = """
        select
            ua.id as Id,
            ua.person_id as PersonId,
            p.organization_id as OrganizationId,
            ua.email as Email,
            ua.password_hash as PasswordHash,
            ua.is_active as IsActive
        from user_accounts ua
        join persons p on p.id = ua.person_id
        where lower(ua.email) = lower(@Email)
          and p.deleted_at is null
        limit 1;
        """;

    public const string GetUserById = """
        select
            ua.id as Id,
            ua.person_id as PersonId,
            p.organization_id as OrganizationId,
            ua.email as Email,
            ua.password_hash as PasswordHash,
            ua.is_active as IsActive
        from user_accounts ua
        join persons p on p.id = ua.person_id
        where ua.id = @UserId
          and p.deleted_at is null
        limit 1;
        """;

    public const string GetSessionByTokenHash = """
        select
            id as Id,
            user_account_id as UserId,
            token_hash as TokenHash,
            expires_at as ExpiresAt,
            revoked_at as RevokedAt,
            created_at as CreatedAt,
            created_by_ip as CreatedByIp,
            replaced_by_token_id as ReplacedBySessionId
        from refresh_tokens
        where token_hash = @TokenHash
        limit 1;
        """;

    public const string InsertSession = """
        insert into refresh_tokens (
            id,
            user_account_id,
            token_hash,
            expires_at,
            created_at,
            created_by_ip
        )
        values (
            @Id,
            @UserId,
            @TokenHash,
            @ExpiresAt,
            @CreatedAt,
            @CreatedByIp
        )
        returning
            id as Id,
            user_account_id as UserId,
            token_hash as TokenHash,
            expires_at as ExpiresAt,
            revoked_at as RevokedAt,
            created_at as CreatedAt,
            created_by_ip as CreatedByIp,
            replaced_by_token_id as ReplacedBySessionId;
        """;

    public const string RevokeSessionByTokenHash = """
        update refresh_tokens
        set revoked_at = @RevokedAt
        where token_hash = @TokenHash and revoked_at is null;
        """;

    public const string RevokeSessionsByUserId = """
        update refresh_tokens
        set revoked_at = @RevokedAt
        where user_account_id = @UserId
          and revoked_at is null;
        """;

    public const string ReplaceSession = """
        update refresh_tokens
        set
            revoked_at = @RevokedAt,
            replaced_by_token_id = @NewSessionId
        where id = @CurrentSessionId
          and revoked_at is null;
        """;

    public const string UpdatePasswordHash = """
        update user_accounts
        set
            password_hash = @PasswordHash,
            updated_at = @UpdatedAt
        where id = @UserId;
        """;

    public const string GetPasswordResetTokenByTokenHash = """
        select
            id as Id,
            user_account_id as UserId,
            token_hash as TokenHash,
            expires_at as ExpiresAt,
            used_at as UsedAt,
            revoked_at as RevokedAt,
            created_at as CreatedAt,
            created_by_ip as CreatedByIp
        from password_reset_tokens
        where token_hash = @TokenHash
        limit 1;
        """;

    public const string InsertPasswordResetToken = """
        insert into password_reset_tokens (
            id,
            user_account_id,
            token_hash,
            expires_at,
            created_at,
            created_by_ip
        )
        values (
            @Id,
            @UserId,
            @TokenHash,
            @ExpiresAt,
            @CreatedAt,
            @CreatedByIp
        )
        returning
            id as Id,
            user_account_id as UserId,
            token_hash as TokenHash,
            expires_at as ExpiresAt,
            used_at as UsedAt,
            revoked_at as RevokedAt,
            created_at as CreatedAt,
            created_by_ip as CreatedByIp;
        """;

    public const string MarkPasswordResetTokenAsUsed = """
        update password_reset_tokens
        set used_at = @UsedAt
        where id = @TokenId and used_at is null and revoked_at is null;
        """;

    public const string RevokePasswordResetTokensByUserId = """
        update password_reset_tokens
        set revoked_at = @RevokedAt
        where user_account_id = @UserId
          and used_at is null
          and revoked_at is null;
        """;
}
