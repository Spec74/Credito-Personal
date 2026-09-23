
        CREATE FUNCTION CREDITO.ufn_FechaGerencial()
        RETURNS DATETIME2(0)
        AS
        BEGIN
            RETURN CONVERT(DATETIME2(0), DATEADD(HOUR, -5, SYSUTCDATETIME()));
        END;
    
