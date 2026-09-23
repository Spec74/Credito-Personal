
/* Congela las metricas cuando el motor inserta un nuevo cierre. */
CREATE   TRIGGER CREDITO.trg_CierreGerencialDetalle_Actividad
ON CREDITO.CierreGerencialDetalle
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE D
       SET D.ClientesNuevosMes = A.ClientesNuevosMes,
           D.MontoClientesNuevosMes = A.MontoClientesNuevosMes,
           D.MontoCobradoMes = A.MontoCobradoMes,
           D.DesembolsosMes = A.DesembolsosMes,
           D.NroOperacionesMes = A.NroOperacionesMes
    FROM CREDITO.CierreGerencialDetalle D
    INNER JOIN inserted I
        ON I.CierreGerencialDetalleId = D.CierreGerencialDetalleId
    INNER JOIN CREDITO.CierreGerencial C
        ON C.CierreGerencialId = D.CierreGerencialId
    OUTER APPLY
    (
        SELECT X.ClientesNuevosMes,
               X.MontoClientesNuevosMes,
               X.MontoCobradoMes,
               X.DesembolsosMes,
               X.NroOperacionesMes
        FROM CREDITO.ufn_ActividadGerencialMensual(C.Periodo, C.FechaCorte, 1) X
        WHERE X.UsuarioId = D.UsuarioId
    ) A;
END;

