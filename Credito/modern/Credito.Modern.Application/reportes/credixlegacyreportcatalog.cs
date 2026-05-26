namespace Credito.Modern.Application.Reportes;

/// <summary>
/// Catálogo de paridad visual PDF ↔ RDLC (título, metadatos, etiquetas de columna).
/// </summary>
public static class CredixLegacyReportCatalog
{
    private static readonly Dictionary<CredixLegacyReportKey, CredixLegacyReportDefinition> Definitions =
        BuildDefinitions();

    private static readonly Dictionary<string, CredixLegacyReportKey> TitleMap =
        BuildTitleMap();

    public static bool TryGetByLegacyTitle(string title, out CredixLegacyReportKey key) =>
        TitleMap.TryGetValue(title.Trim(), out key);

    public static CredixLegacyReportDefinition Get(CredixLegacyReportKey key) =>
        Definitions.TryGetValue(key, out var def)
            ? def
            : throw new KeyNotFoundException($"Informe no registrado: {key}");

    public static IReadOnlyList<CredixLegacyPdfDocument.MetadataLine> BuildMetadata(
        CredixLegacyReportKey key,
        CredixLegacyReportContext ctx)
    {
        var lines = new List<CredixLegacyPdfDocument.MetadataLine>();
        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                lines.Add(new CredixLegacyPdfDocument.MetadataLine(label, value));
        }

        switch (key)
        {
            case CredixLegacyReportKey.CobranzaPagos:
            case CredixLegacyReportKey.CobroDiario:
            case CredixLegacyReportKey.MorosidadGestor:
                Add("Fecha: ", ctx.Fecha ?? ctx.FechaReporte);
                Add("Agente: ", ctx.Agente);
                Add("Caja: ", ctx.Caja);
                Add("Saldo vencido: ", ctx.SaldoVencido);
                Add("Saldo moroso: ", ctx.SaldoMoroso);
                Add("N° clientes: ", ctx.NroClientes);
                break;
            case CredixLegacyReportKey.ClientesNuevosMes:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                if (!string.IsNullOrWhiteSpace(ctx.FechaIni) && !string.IsNullOrWhiteSpace(ctx.FechaFin))
                    lines.Add(new CredixLegacyPdfDocument.MetadataLine(
                        "Periodo: ",
                        $"{ctx.FechaIni} al {ctx.FechaFin}"));
                break;
            case CredixLegacyReportKey.CajaDiario:
            case CredixLegacyReportKey.ComprobantesCajaChica:
            case CredixLegacyReportKey.CreditoRentabilidad:
            case CredixLegacyReportKey.ReporteCredito:
            case CredixLegacyReportKey.SaldoCarteraCajaDiario:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Desde: ", ctx.FechaIni);
                Add("Hasta: ", ctx.FechaFin);
                break;
            case CredixLegacyReportKey.ClientesTopeCredito:
            case CredixLegacyReportKey.ClientesBloqueados:
            case CredixLegacyReportKey.ClientesInactivos:
                Add("Fecha: ", ctx.Fecha);
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                break;
            case CredixLegacyReportKey.CreditoObservado:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                break;
            default:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Estado: ", ctx.Estado);
                Add("Desde: ", ctx.FechaIni);
                Add("Hasta: ", ctx.FechaFin);
                Add("Fecha: ", ctx.Fecha ?? ctx.FechaReporte);
                break;
        }

        return lines;
    }

    private static Dictionary<string, CredixLegacyReportKey> BuildTitleMap() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Cobro diario"] = CredixLegacyReportKey.CobroDiario,
            ["Morosidad por gestor"] = CredixLegacyReportKey.MorosidadGestor,
            ["Clientes inactivos"] = CredixLegacyReportKey.ClientesInactivos,
            ["Clientes bloqueados"] = CredixLegacyReportKey.ClientesBloqueados,
            ["Clientes tope credito"] = CredixLegacyReportKey.ClientesTopeCredito,
            ["Credito observado"] = CredixLegacyReportKey.CreditoObservado,
            ["Créditos observados"] = CredixLegacyReportKey.CreditoObservado,
            ["Crédito observado"] = CredixLegacyReportKey.CreditoObservado,
            ["Créditos observados"] = CredixLegacyReportKey.CreditoObservado,
            ["Crédito observado"] = CredixLegacyReportKey.CreditoObservado,
            ["Clientes nuevos mes"] = CredixLegacyReportKey.ClientesNuevosMes,
            ["Credito condonado"] = CredixLegacyReportKey.CreditoCondonado,
            ["Créditos condonados"] = CredixLegacyReportKey.CreditoCondonado,
            ["Crédito condonado"] = CredixLegacyReportKey.CreditoCondonado,
            ["Créditos condonados"] = CredixLegacyReportKey.CreditoCondonado,
            ["Crédito condonado"] = CredixLegacyReportKey.CreditoCondonado,
            ["Credito rentabilidad"] = CredixLegacyReportKey.CreditoRentabilidad,
            ["Credito aprobacion"] = CredixLegacyReportKey.CreditoAprobacion,
            ["Creditos activos"] = CredixLegacyReportKey.CreditosActivos,
            ["Creditos cierres"] = CredixLegacyReportKey.CreditosCierres,
            ["Creditos morosos pagados"] = CredixLegacyReportKey.CreditosMorososPagados,
            ["Reporte credito"] = CredixLegacyReportKey.ReporteCredito,
            ["Caja diario"] = CredixLegacyReportKey.CajaDiario,
            ["Credito vencido"] = CredixLegacyReportKey.CreditoVencido,
            ["Movimiento caja anulado"] = CredixLegacyReportKey.MovimientoCajaAnulado,
            ["Saldo cartera caja diario"] = CredixLegacyReportKey.SaldoCarteraCajaDiario,
            ["Comprobantes caja chica"] = CredixLegacyReportKey.ComprobantesCajaChica,
            ["Cajas asignadas"] = CredixLegacyReportKey.CajasAsignadas,
            ["Cobro diario detalle"] = CredixLegacyReportKey.CobroDiarioDetalle,
            ["Cobranza pagos por gestor"] = CredixLegacyReportKey.CobranzaPagos,
            ["Reporte de cobranza por gestor"] = CredixLegacyReportKey.CobranzaPagos,
            ["Cobranza pagos por gestor"] = CredixLegacyReportKey.CobranzaPagos,
            ["Reporte de cobranza por gestor"] = CredixLegacyReportKey.CobranzaPagos,
            ["Movimiento credito"] = CredixLegacyReportKey.MovimientoCredito,
            ["Listar saldo cartera"] = CredixLegacyReportKey.ListarSaldoCartera,
            ["Aval persona"] = CredixLegacyReportKey.AvalPersona,
            ["Saldos caja"] = CredixLegacyReportKey.SaldosCaja,
            ["Movimiento boveda"] = CredixLegacyReportKey.MovimientoBoveda,
            ["Central riesgo generar"] = CredixLegacyReportKey.CentralRiesgoGenerar,
            ["Pagos no verificados"] = CredixLegacyReportKey.PagosNoVerificados,
            ["Reporte stock"] = CredixLegacyReportKey.ReporteStock,
            ["Stock anulados"] = CredixLegacyReportKey.StockAnulados,
            ["Lista precio"] = CredixLegacyReportKey.ListaPrecio,
            ["Kardex"] = CredixLegacyReportKey.Kardex,
            ["Rentabilidad venta"] = CredixLegacyReportKey.RentabilidadVenta,
        };

    private static Dictionary<CredixLegacyReportKey, CredixLegacyReportDefinition> BuildDefinitions() =>
        new()
        {
            [CredixLegacyReportKey.CobroDiario] = Def(
                "COBRO DIARIO",
                CobroDiarioCols()),
            [CredixLegacyReportKey.MorosidadGestor] = Def(
                "REPORTE DE MOROSIDAD",
                CobroDiarioCols()),
            [CredixLegacyReportKey.ClientesInactivos] = Def(
                "CLIENTES INACTIVOS",
                Cols(
                    L("PersonaId", "Persona"),
                    L("Agente", "Agente"),
                    L("Codigo", "Código"),
                    L("Dni", "DNI"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    L("Celular", "Celular"),
                    L("Calificacion", "Calificación"),
                    L("DireccionNegocio", "Dir. negocio"),
                    L("DireccionNegocioRef", "Dir. neg. ref."))),
            [CredixLegacyReportKey.ClientesBloqueados] = Def(
                "CLIENTES BLOQUEADOS",
                Cols(
                    L("Agente", "Agente"),
                    L("NumeroDocumento", "N° documento"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    L("Celular", "Celular"),
                    L("Calificacion", "Calificación"),
                    L("Nota", "Nota"))),
            [CredixLegacyReportKey.ClientesTopeCredito] = Def(
                "CLIENTES TOPE CRÉDITO",
                Cols(
                    L("Agente", "Agente"),
                    L("NumeroDocumento", "N° documento"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    L("Celular", "Celular"),
                    L("Calificacion", "Calificación"),
                    N("TopeCredito", "Tope crédito"),
                    L("Nota", "Nota"))),
            [CredixLegacyReportKey.CreditoObservado] = Def(
                "CRÉDITOS OBSERVADOS",
                ObservadoCols()),
            [CredixLegacyReportKey.ClientesNuevosMes] = Def(
                "CLIENTES NUEVOS DEL MES",
                ObservadoCols()),
            [CredixLegacyReportKey.CreditoCondonado] = Def(
                "CRÉDITOS CONDONADOS",
                Cols(
                    L("OficinaId", "Oficina Id"),
                    L("Oficina", "Oficina"),
                    N("CreditoId", "Cred"),
                    L("Cliente", "Cliente"),
                    L("FechaPrimerPago", "F. primer pago"),
                    L("FechaVencimiento", "F. vencimiento"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    N("MontoCondonado", "Monto condonado"),
                    L("AgenteId", "Agente Id"),
                    L("Agente", "Agente"),
                    L("Observacion", "Observación"))),
            [CredixLegacyReportKey.CreditoRentabilidad] = Def(
                "RENTABILIDAD DE CRÉDITOS",
                Cols(
                    N("CreditoId", "Cred"),
                    L("Oficina", "Oficina"),
                    L("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    L("FechaDesembolso", "F. desembolso"),
                    L("FechaPago", "F. pago"),
                    N("NumeroCuotas", "N° cuotas"),
                    L("FormaPago", "Forma pago"),
                    L("Estado", "Estado"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    N("SumCuota", "Σ cuota"),
                    N("CuotasPagadas", "Cuotas pag."),
                    N("MontoGastosAdm", "Gastos adm."),
                    N("SumInteres", "Σ interés"),
                    N("SumMora", "Σ mora"),
                    N("SumPago", "Σ pago"))),
            [CredixLegacyReportKey.CreditoAprobacion] = Def(
                "CRÉDITOS APROBADOS",
                Cols(
                    N("CreditoId", "Cred"),
                    L("Oficina", "Oficina"),
                    L("Cliente", "Cliente"),
                    L("FechaAprobacion", "F. aprobación"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    N("NumeroCuotas", "N° cuotas"),
                    N("MontoDesembolso", "Desembolso"),
                    L("Gestor", "Gestor"))),
            [CredixLegacyReportKey.CreditosActivos] = Def(
                "CRÉDITOS ACTIVOS",
                CreditosActivosCols()),
            [CredixLegacyReportKey.CreditosCierres] = Def(
                "CRÉDITOS CIERRES",
                Cols(
                    N("CreditoId", "Cred"),
                    L("Estado", "Estado"),
                    L("Agente", "Agente"),
                    L("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    L("FormaPago", "Forma pago"),
                    N("NumeroCuotas", "N° cuotas"),
                    N("Interes", "Interés"),
                    N("MontoGastosAdm", "Gastos adm."),
                    L("CentralRiesgo", "Cent. riesgo"),
                    L("FechaPrimerPago", "F. primer pago"),
                    L("FechaVencimiento", "F. vencimiento"),
                    N("SumAmortizacion", "Σ capital"),
                    N("SumInteres", "Σ interés"),
                    N("SumCuota", "Σ cuota"),
                    N("SumNroCuota", "Σ n° cuota"))),
            [CredixLegacyReportKey.CreditosMorososPagados] = Def(
                "CRÉDITOS MOROSOS PAGADOS",
                Cols(
                    N("CreditoId", "Cred"),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    L("FormaPago", "Forma pago"),
                    N("NumeroCuotas", "N° cuotas"),
                    N("MontoGastosAdm", "Gastos adm."),
                    L("CentralRiesgo", "Cent. riesgo"),
                    L("FechaPrimerPago", "F. primer pago"),
                    L("FechaVencimiento", "F. vencimiento"),
                    L("FechaPagado", "F. pagado"),
                    L("Agente", "Agente"))),
            [CredixLegacyReportKey.ReporteCredito] = Def(
                "REPORTE DE CRÉDITOS",
                Cols(
                    L("Producto", "Producto"),
                    L("Cliente", "Cliente"),
                    N("CreditoId", "Cred"),
                    L("FechaDesembolso", "F. desembolso"),
                    L("FechaVcto", "F. vcto"),
                    L("FormaPago", "Forma pago"),
                    N("NumeroCuotas", "N° cuotas"),
                    N("Interes", "Interés"),
                    L("Estado", "Estado"),
                    N("MontoProducto", "Monto producto"),
                    N("MontoInicial", "Monto inicial"),
                    N("MontoCredito", "Monto crédito"),
                    L("TipoGastoAdm", "Tipo gasto adm."),
                    N("MontoGastosAdm", "Gastos adm."),
                    N("MontoDesembolso", "Desembolso"),
                    L("Observacion", "Observación"))),
            [CredixLegacyReportKey.CajaDiario] = Def(
                "REPORTE CAJA DIARIO",
                Cols(
                    N("CajaDiarioId", "Id"),
                    L("Oficina", "Oficina"),
                    L("Caja", "Caja"),
                    L("Agente", "Agente"),
                    N("SaldoInicial", "Saldo inicial"),
                    N("Entradas", "Entradas"),
                    N("Salidas", "Salidas"),
                    N("SaldoFinal", "Saldo final"),
                    L("FechaIniOperacion", "Inicio op."),
                    L("FechaFinOperacion", "Fin op."))),
            [CredixLegacyReportKey.CreditoVencido] = Def(
                "CRÉDITO VENCIDO",
                Cols(
                    L("Gestor", "Gestor"),
                    N("CreditoId", "Cred"),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    L("FormaPago", "Forma pago"),
                    L("FechaVencimiento", "F. vencimiento"),
                    N("CreditoVencido", "Vencido"),
                    N("VencidoMenor60", "< 60 días"),
                    N("VencidoMayor60", "> 60 días"),
                    N("VencidoIrrecuperable", "Irrecuperable"))),
            [CredixLegacyReportKey.MovimientoCajaAnulado] = Def(
                "MOVIMIENTOS CAJA ANULADOS",
                Cols(
                    N("MovimientoCajaId", "Id"),
                    L("Operacion", "Operación"),
                    N("ImportePago", "Importe"),
                    L("Persona", "Persona"),
                    L("Descripcion", "Descripción"),
                    L("FechaReg", "F. registro"),
                    L("UsuarioRegistro", "Usuario reg."),
                    L("MotivoAnulacion", "Motivo"),
                    L("FechaAnulacion", "F. anulación"),
                    L("UsuarioAnulacion", "Usuario anul."))),
            [CredixLegacyReportKey.SaldoCarteraCajaDiario] = Def(
                "SALDO CARTERA Y CAJA DIARIO",
                SaldoCarteraCols()),
            [CredixLegacyReportKey.ComprobantesCajaChica] = Def(
                "COMPROBANTES CAJA CHICA",
                Cols(
                    L("Gasto", "Gasto"),
                    L("Fecha", "Fecha"),
                    L("Documento", "Documento"),
                    L("Serie", "Serie"),
                    L("Numero", "Número"),
                    L("RUC", "RUC"),
                    L("RazonSocial", "Razón social"),
                    L("DetalleGasto", "Detalle"),
                    N("Importe", "Importe"))),
            [CredixLegacyReportKey.CajasAsignadas] = Def(
                "CAJAS ASIGNADAS",
                Cols(
                    N("CajaDiarioId", "Id"),
                    L("Caja", "Caja"),
                    L("Modo", "Modo"),
                    L("Cajero", "Cajero"),
                    L("FechaIniOperacion", "Inicio"),
                    L("FechaFinOperacion", "Fin"),
                    N("SaldoInicial", "Saldo ini."),
                    N("Salidas", "Salidas"),
                    N("Entradas", "Entradas"),
                    N("SaldoFinal", "Saldo final"),
                    L("Resumen", "Resumen"))),
            [CredixLegacyReportKey.CobroDiarioDetalle] = Def(
                "COBRO DIARIO DETALLE",
                CobranzaDetalleCols()),
            [CredixLegacyReportKey.CobranzaPagos] = Def(
                "REPORTE DE COBRANZA POR GESTOR",
                CobranzaDetalleCols()),
            [CredixLegacyReportKey.MovimientoCredito] = Def(
                "MOVIMIENTO CRÉDITO",
                Cols(
                    N("MovimientoCajaId", "Id"),
                    L("Fecha", "Fecha"),
                    L("Operacion", "Operación"),
                    L("Glosa", "Glosa"),
                    N("ImportePago", "Importe"),
                    N("Saldo", "Saldo")),
                landscape: false),
            [CredixLegacyReportKey.ListarSaldoCartera] = Def(
                "SALDO CARTERA",
                Cols(
                    L("AgenteId", "Agente Id"),
                    L("OficinaId", "Oficina Id"),
                    N("NroDesembolsos", "N° desembolsos"),
                    N("MontoDesembolsos", "Monto desembolsos"),
                    N("SaldoCartera", "Saldo cartera"),
                    N("NroClientesSaldoCartera", "Cli. cartera"),
                    N("SaldoMoraCartera", "Saldo mora"),
                    N("NroClientesSaldoMoraCartera", "Cli. mora"),
                    N("SaldoVencido", "Saldo vencido"),
                    N("SaldoMorosidad", "Saldo morosidad"),
                    N("NroClientesNuevos", "Cli. nuevos"),
                    L("FechaCierre", "F. cierre"))),
            [CredixLegacyReportKey.AvalPersona] = Def(
                "AVAL PERSONA",
                Cols(
                    L("Grupo", "Grupo"),
                    N("CreditoId", "Cred"),
                    N("MontoCredito", "Monto"),
                    L("Estado", "Estado"),
                    L("Persona", "Persona"),
                    L("Dni", "DNI"),
                    L("Celular", "Celular"))),
            [CredixLegacyReportKey.SaldosCaja] = Def(
                "SALDOS CAJA",
                Cols(
                    N("MovimientoCajaId", "Id"),
                    L("Operacion", "Operación"),
                    L("FechaReg", "Fecha"),
                    L("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    N("ImportePago", "Importe"),
                    L("IndEntrada", "Entrada"),
                    L("Glosa", "Glosa"),
                    L("TipoPago", "Tipo pago"))),
            [CredixLegacyReportKey.MovimientoBoveda] = Def(
                "MOVIMIENTO BÓVEDA",
                Cols(
                    N("MovimientoBovedaId", "Id"),
                    L("FechaReg", "Fecha"),
                    L("CodOperacion", "Cód. op."),
                    L("Glosa", "Glosa"),
                    N("Entrada", "Entrada"),
                    N("Salida", "Salida"),
                    L("TipoPago", "Tipo pago"),
                    L("Agente", "Agente")),
                landscape: false),
            [CredixLegacyReportKey.CentralRiesgoGenerar] = Def(
                "CENTRAL DE RIESGO",
                Cols(
                    L("Anio", "Año"),
                    L("Mes", "Mes"),
                    N("CreditoId", "Cred"),
                    L("Periodo", "Periodo"),
                    L("Entidad", "Entidad"),
                    L("TipoDoc", "Tipo doc."),
                    L("NumDoc", "N° doc."),
                    L("RazonSocial", "Razón social"),
                    L("ApePat", "Ap. paterno"),
                    L("ApeMat", "Ap. materno"),
                    L("Nombres", "Nombres"),
                    L("TipoPersona", "Tipo persona"),
                    L("ModalidadCredito", "Modalidad"),
                    N("DeudaMenor30", "Deuda < 30"),
                    N("DeudaMayor30", "Deuda > 30"),
                    L("Calificacion", "Calificación"),
                    N("DiasAtrazo", "Días atraso"),
                    L("Direccion", "Dirección"),
                    L("celular", "Celular"))),
            [CredixLegacyReportKey.PagosNoVerificados] = Def(
                "PAGOS NO VERIFICADOS",
                Cols(
                    N("MovimientoCajaId", "Id"),
                    L("Cliente", "Cliente"),
                    L("Movimiento", "Movimiento"),
                    N("ImportePago", "Importe"),
                    L("TipoPago", "Tipo pago"),
                    L("FechaTransferencia", "F. transferencia"),
                    L("Registro", "Registro"))),
            [CredixLegacyReportKey.ReporteStock] = Def(
                "REPORTE STOCK",
                Cols(
                    N("Nro", "Nro"),
                    L("TipoArticulo", "Tipo"),
                    N("ArticuloId", "Art. Id"),
                    L("Articulo", "Artículo"),
                    N("Stock", "Stock"),
                    L("Series", "Series"))),
            [CredixLegacyReportKey.StockAnulados] = Def(
                "STOCK ANULADOS",
                Cols(
                    N("MovimientoId", "Mov. Id"),
                    L("Movimiento", "Movimiento"),
                    L("Observacion", "Observación"),
                    L("Fecha", "Fecha"),
                    N("Cantidad", "Cantidad"),
                    L("Detalle", "Detalle"))),
            [CredixLegacyReportKey.ListaPrecio] = Def(
                "LISTA DE PRECIOS",
                Cols(
                    N("ArticuloId", "Art. Id"),
                    L("TipoArticulo", "Tipo"),
                    L("ArticuloDes", "Artículo"),
                    N("Monto", "Monto"),
                    N("Descuento", "Descuento"),
                    N("PuntosCanje", "Puntos")),
                landscape: false),
            [CredixLegacyReportKey.Kardex] = Def(
                "KARDEX",
                Cols(
                    N("MovimientoDetId", "Det. Id"),
                    L("Fecha", "Fecha"),
                    L("Concepto", "Concepto"),
                    N("CantEnt", "Cant. ent."),
                    N("PUEnt", "PU ent."),
                    N("TotalEnt", "Total ent."),
                    N("CantSal", "Cant. sal."),
                    N("PUSal", "PU sal."),
                    N("TotalSal", "Total sal."),
                    N("CantSaldo", "Cant. saldo"),
                    N("PUSaldo", "PU saldo"),
                    N("TotalSaldo", "Total saldo")),
                landscape: false),
            [CredixLegacyReportKey.RentabilidadVenta] = Def(
                "RENTABILIDAD VENTA",
                Cols(
                    N("Nro", "Nro"),
                    L("Codigo", "Código"),
                    L("Articulo", "Artículo"),
                    N("MovimientoId", "Mov. Id"),
                    L("FechaEnt", "F. entrada"),
                    N("PrecioEnt", "P. entrada"),
                    N("OrdenVentaId", "Orden"),
                    L("FechaSal", "F. salida"),
                    N("PrecioSal", "P. salida"),
                    L("Modalidad", "Modalidad"),
                    N("Rentabilidad", "Rentabilidad"),
                    L("Cliente", "Cliente"))),
        };

    private static CredixLegacyColumnSpec[] CobranzaDetalleCols() =>
        Cols(
            N("Nro", "Nro"),
            L("Cliente", "Cliente"),
            L("FormaPago", "Tipo"),
            N("MontoCredito", "Crédito"),
            N("Interes", "Interés"),
            N("MontoTotal", "Monto Total"),
            L("FechaPrimerPago", "1er Pago"),
            L("FechaVencimiento", "Vencimiento"),
            N("TotalPago", "Total Pagado"),
            N("Saldo", "Saldo"),
            N("DiasAtrazoMora", "Días mora"),
            L("Pagos", "Pagos"));

    private static CredixLegacyColumnSpec[] CobroDiarioCols() =>
        Cols(
            N("Nro", "Nro"),
            N("Orden", "Ord."),
            N("CreditoId", "Cred"),
            L("Cliente", "Cliente"),
            L("Celular", "Celular"),
            N("MontoCredito", "Monto crédito"),
            N("Interes", "Interés"),
            N("CuotaPlan", "Cuota plan"),
            N("Saldo", "Saldo"),
            N("DiasAtrazo", "Días atraso"),
            N("NroCuotasPen", "Cuotas pend."),
            N("CuotaTotal", "Cuota total"),
            L("Direccion", "Dirección"),
            L("FechaPago", "F. pago"),
            L("FechaPrimerPago", "F. 1er pago"),
            L("FechaVencimiento", "F. vcto"),
            N("Mora", "Mora"),
            N("MontoTotal", "Monto total"),
            L("Negocio", "Negocio"),
            L("FormaPago", "Forma pago"),
            N("TopeCredito", "Tope crédito"),
            L("ClasificacionRiesgoSBS", "Clasif. SBS"));

    private static CredixLegacyColumnSpec[] ObservadoCols() =>
        Cols(
            L("OficinaId", "Of. Id"),
            L("Oficina", "Oficina"),
            N("CreditoId", "Cred"),
            L("Cliente", "Cliente"),
            L("FechaPrimerPago", "F. 1er pago"),
            L("FechaVencimiento", "F. vcto"),
            N("MontoCredito", "Monto crédito"),
            N("Interes", "Interés"),
            L("AgenteId", "Ag. Id"),
            L("Agente", "Agente"),
            L("Observacion", "Observación"),
            L("TramiteAdm", "Trámite adm."),
            L("CentralRiesgo", "Cent. riesgo"));

    private static CredixLegacyColumnSpec[] CreditosActivosCols() =>
        Cols(
            N("Nro", "Nro"),
            L("Estado", "Estado"),
            L("Agente", "Agente"),
            N("CreditoId", "Cred"),
            L("Codigo", "Código"),
            L("Cliente", "Cliente"),
            N("MontoCredito", "Monto crédito"),
            L("FormaPago", "Forma pago"),
            N("NumeroCuotas", "N° cuotas"),
            N("Interes", "Interés"),
            N("MontoInteres", "Monto int."),
            N("MontoCreditoTotal", "Total crédito"),
            N("MontoGastosAdm", "Gastos adm."),
            L("CentralRiesgo", "Cent. riesgo"),
            L("FechaPrimerPago", "F. 1er pago"),
            L("FechaVencimiento", "F. vcto"),
            N("NroCuotasPagado", "Cuotas pag."),
            N("Pagado", "Pagado"),
            N("InteresPagado", "Int. pagado"),
            N("NroCuotasPen", "Cuotas pend."),
            N("SaldoCapital", "Saldo cap."),
            N("SaldoInteres", "Saldo int."),
            N("Saldo", "Saldo"),
            N("DiasAtrazo", "Días atraso"),
            N("Mora", "Mora"));

    private static CredixLegacyColumnSpec[] SaldoCarteraCols() =>
        Cols(
            L("AgenteId", "Ag. Id"),
            L("Oficina", "Oficina"),
            L("Caja", "Caja"),
            L("Agente", "Agente"),
            L("FechaCierreIni", "Cierre ini."),
            N("SalidasIni", "Salidas ini."),
            N("MontoCobradoIni", "Cobrado ini."),
            N("PocentajeCobroIni", "% cobro ini."),
            N("SaldoCarteraSinMoraIni", "Cartera s/mora ini."),
            N("NroClientesCarteraSinMoraIni", "Cli. s/mora ini."),
            N("SaldoMoraCarteraIni", "Mora cartera ini."),
            N("NroClientesSaldoMoraCarteraIni", "Cli. mora ini."),
            N("NroClientesNuevosIni", "Cli. nuevos ini."),
            N("SaldoVencidoIni", "Vencido ini."),
            N("SaldoMorosidadIni", "Morosidad ini."),
            L("FechaCierreFin", "Cierre fin"),
            N("SalidasFin", "Salidas fin"),
            N("MontoCobradoFin", "Cobrado fin"),
            N("PocentajeCobroFin", "% cobro fin"),
            N("SaldoCarteraSinMoraFin", "Cartera s/mora fin"),
            N("NroClientesCarteraSinMoraFin", "Cli. s/mora fin."),
            N("SaldoMoraCarteraFin", "Mora cartera fin"),
            N("NroClientesSaldoMoraCarteraFin", "Cli. mora fin."),
            N("NroClientesNuevosFin", "Cli. nuevos fin."),
            N("SaldoVencidoFin", "Vencido fin"),
            N("SaldoMorosidadFin", "Morosidad fin"));

    private static CredixLegacyReportDefinition Def(
        string title,
        CredixLegacyColumnSpec[] columns,
        bool landscape = true) =>
        new(title, columns, landscape);

    private static CredixLegacyColumnSpec[] Cols(params CredixLegacyColumnSpec[] cols) => cols;

    private static CredixLegacyColumnSpec L(string csv, string label) =>
        new(csv, label, CredixColumnAlign.Left, CredixColumnWeights.For(csv, CredixColumnAlign.Left));

    private static CredixLegacyColumnSpec N(string csv, string label) =>
        new(csv, label, CredixColumnAlign.Right, CredixColumnWeights.For(csv, CredixColumnAlign.Right));
}

/// <summary>Definición de layout PDF para un informe.</summary>
public sealed class CredixLegacyReportDefinition
{
    public CredixLegacyReportDefinition(
        string title,
        IReadOnlyList<CredixLegacyColumnSpec> columns,
        bool landscape = true)
    {
        Title = title;
        Columns = columns;
        Landscape = landscape;
    }

    public string Title { get; }
    public IReadOnlyList<CredixLegacyColumnSpec> Columns { get; }
    public bool Landscape { get; }
}
