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
        TitleMap.TryGetValue(CredixReportTitle.Normalize(title), out key);

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
                Add("Oficina: ", ctx.Oficina);
                Add("Fecha: ", ctx.Fecha ?? ctx.FechaReporte);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Caja: ", ctx.Caja);
                Add("Saldo vencido: ", ctx.SaldoVencido);
                Add("Saldo moroso: ", ctx.SaldoMoroso);
                Add("N° clientes: ", ctx.NroClientes);
                break;
            case CredixLegacyReportKey.ClientesNuevosMes:
            case CredixLegacyReportKey.CajaDiario:
            case CredixLegacyReportKey.ComprobantesCajaChica:
            case CredixLegacyReportKey.CreditoRentabilidad:
            case CredixLegacyReportKey.ReporteCredito:
            case CredixLegacyReportKey.SaldoCarteraCajaDiario:
            case CredixLegacyReportKey.CreditoCondonado:
            case CredixLegacyReportKey.CreditosActivos:
            case CredixLegacyReportKey.CreditosCierres:
            case CredixLegacyReportKey.CreditosMorososPagados:
            case CredixLegacyReportKey.ClientesInactivos:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                AddPeriodo(ctx.FechaIni, ctx.FechaFin);
                Add("Estado: ", ctx.Estado);
                break;
            case CredixLegacyReportKey.ClientesTopeCredito:
            case CredixLegacyReportKey.ClientesBloqueados:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Fecha: ", ctx.Fecha);
                break;
            case CredixLegacyReportKey.CreditoObservado:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                break;
            case CredixLegacyReportKey.CreditoMorosidad:
                Add("Oficina: ", ctx.Oficina);
                Add("Fecha corte: ", ctx.Fecha);
                break;
            case CredixLegacyReportKey.CreditoAprobacion:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Fecha aprobación: ", ctx.Fecha);
                break;
            case CredixLegacyReportKey.CentralRiesgoGenerar:
            case CredixLegacyReportKey.ListarSaldoCartera:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Periodo: ", ctx.Fecha ?? ctx.FechaReporte);
                break;
            default:
                Add("Oficina: ", ctx.Oficina);
                Add("Agente: ", ctx.Agente ?? ctx.Gestor);
                Add("Caja: ", ctx.Caja);
                Add("Estado: ", ctx.Estado);
                AddPeriodo(ctx.FechaIni, ctx.FechaFin);
                Add("Fecha: ", ctx.Fecha ?? ctx.FechaReporte);
                Add("Referencia: ", ctx.Referencia);
                break;
        }

        return lines;

        void AddPeriodo(string? ini, string? fin)
        {
            if (!string.IsNullOrWhiteSpace(ini) && !string.IsNullOrWhiteSpace(fin))
                Add("Periodo: ", $"{ini} al {fin}");
            else
            {
                Add("Desde: ", ini);
                Add("Hasta: ", fin);
            }
        }
    }

    private static Dictionary<string, CredixLegacyReportKey> BuildTitleMap()
    {
        var map = new Dictionary<string, CredixLegacyReportKey>(StringComparer.Ordinal);
        void Add(string title, CredixLegacyReportKey key) =>
            map[CredixReportTitle.Normalize(title)] = key;

        Add("Cobro diario", CredixLegacyReportKey.CobroDiario);
        Add("Morosidad por gestor", CredixLegacyReportKey.MorosidadGestor);
        Add("Clientes inactivos", CredixLegacyReportKey.ClientesInactivos);
        Add("Clientes bloqueados", CredixLegacyReportKey.ClientesBloqueados);
        Add("Clientes tope credito", CredixLegacyReportKey.ClientesTopeCredito);
        Add("Credito observado", CredixLegacyReportKey.CreditoObservado);
        Add("Creditos observados", CredixLegacyReportKey.CreditoObservado);
        Add("Clientes nuevos mes", CredixLegacyReportKey.ClientesNuevosMes);
        Add("Clientes nuevos del mes", CredixLegacyReportKey.ClientesNuevosMes);
        Add("Credito condonado", CredixLegacyReportKey.CreditoCondonado);
        Add("Creditos condonados", CredixLegacyReportKey.CreditoCondonado);
        Add("Credito rentabilidad", CredixLegacyReportKey.CreditoRentabilidad);
        Add("Rentabilidad credito", CredixLegacyReportKey.CreditoRentabilidad);
        Add("Credito aprobacion", CredixLegacyReportKey.CreditoAprobacion);
        Add("Aprobacion credito", CredixLegacyReportKey.CreditoAprobacion);
        Add("Creditos activos", CredixLegacyReportKey.CreditosActivos);
        Add("Creditos cierres", CredixLegacyReportKey.CreditosCierres);
        Add("Creditos morosos pagados", CredixLegacyReportKey.CreditosMorososPagados);
        Add("Morosos pagados", CredixLegacyReportKey.CreditosMorososPagados);
        Add("Reporte credito", CredixLegacyReportKey.ReporteCredito);
        Add("Caja diario", CredixLegacyReportKey.CajaDiario);
        Add("Credito vencido", CredixLegacyReportKey.CreditoVencido);
        Add("Movimiento caja anulado", CredixLegacyReportKey.MovimientoCajaAnulado);
        Add("Saldo cartera caja diario", CredixLegacyReportKey.SaldoCarteraCajaDiario);
        Add("Comprobantes caja chica", CredixLegacyReportKey.ComprobantesCajaChica);
        Add("Cajas asignadas", CredixLegacyReportKey.CajasAsignadas);
        Add("Cobro diario detalle", CredixLegacyReportKey.CobroDiarioDetalle);
        Add("Cobranza pagos por gestor", CredixLegacyReportKey.CobranzaPagos);
        Add("Reporte de cobranza por gestor", CredixLegacyReportKey.CobranzaPagos);
        Add("Movimiento credito", CredixLegacyReportKey.MovimientoCredito);
        Add("Listar saldo cartera", CredixLegacyReportKey.ListarSaldoCartera);
        Add("Saldo cartera", CredixLegacyReportKey.ListarSaldoCartera);
        Add("Aval persona", CredixLegacyReportKey.AvalPersona);
        Add("Avales", CredixLegacyReportKey.AvalPersona);
        Add("Saldos caja", CredixLegacyReportKey.SaldosCaja);
        Add("Movimiento boveda", CredixLegacyReportKey.MovimientoBoveda);
        Add("Central riesgo generar", CredixLegacyReportKey.CentralRiesgoGenerar);
        Add("Central de riesgo", CredixLegacyReportKey.CentralRiesgoGenerar);
        Add("Pagos no verificados", CredixLegacyReportKey.PagosNoVerificados);
        Add("Reporte stock", CredixLegacyReportKey.ReporteStock);
        Add("Stock anulados", CredixLegacyReportKey.StockAnulados);
        Add("Lista precio", CredixLegacyReportKey.ListaPrecio);
        Add("Lista de precios", CredixLegacyReportKey.ListaPrecio);
        Add("Kardex", CredixLegacyReportKey.Kardex);
        Add("Rentabilidad venta", CredixLegacyReportKey.RentabilidadVenta);
        Add("Morosidad credito", CredixLegacyReportKey.CreditoMorosidad);
        return map;
    }

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
                    L("Agente", "Agente"),
                    C("Dni", "DNI"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    C("Celular", "Celular"),
                    C("Calificacion", "Cal."),
                    C("ClasificacionRiesgoSBS", "SBS"),
                    C("Depurado", "Depurado"),
                    L("DireccionNegocio", "Dir. negocio"),
                    L("DireccionNegocioRef", "Dir. neg. ref."),
                    N("MontoCredito", "Monto crédito"),
                    C("TotalCreditos", "Cant. créditos"),
                    C("FechaCancelacion", "Fecha cancelación"),
                    C("DiasInactividad", "Días inact."),
                    N("TopeCredito", "Tope crédito"))),
            [CredixLegacyReportKey.ClientesBloqueados] = Def(
                "CLIENTES BLOQUEADOS",
                Cols(
                    L("Agente", "Agente"),
                    C("NumeroDocumento", "N° documento"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    C("Celular", "Celular"),
                    C("Calificacion", "Calificación"),
                    L("Nota", "Nota"))),
            [CredixLegacyReportKey.ClientesTopeCredito] = Def(
                "CLIENTES TOPE CRÉDITO",
                Cols(
                    L("Agente", "Agente"),
                    C("NumeroDocumento", "N° documento"),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    L("DireccionRef", "Dir. ref."),
                    C("Celular", "Celular"),
                    C("Calificacion", "Calificación"),
                    N("TopeCredito", "Tope crédito"),
                    L("Nota", "Nota"))),
            [CredixLegacyReportKey.CreditoObservado] = Def(
                "CRÉDITOS OBSERVADOS",
                ObservadoCols()),
            [CredixLegacyReportKey.ClientesNuevosMes] = Def(
                "CLIENTES NUEVOS DEL MES",
                ClientesNuevosCols()),
            [CredixLegacyReportKey.CreditoCondonado] = Def(
                "CRÉDITOS CONDONADOS",
                Cols(
                    L("Oficina", "Oficina"),
                    C("CreditoId", "N° créd."),
                    L("Cliente", "Cliente"),
                    C("FechaPrimerPago", "F. primer pago"),
                    C("FechaVencimiento", "F. vencimiento"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    N("MontoCondonado", "Monto condonado"),
                    L("Agente", "Agente"),
                    L("Observacion", "Observación"))),
            [CredixLegacyReportKey.CreditoRentabilidad] = Def(
                "RENTABILIDAD DE CRÉDITOS",
                Cols(
                    C("CreditoId", "N° créd."),
                    L("Oficina", "Oficina"),
                    C("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    C("FechaDesembolso", "F. desembolso"),
                    C("FechaPago", "F. pago"),
                    C("NumeroCuotas", "N° cuotas"),
                    C("FormaPago", "Forma pago"),
                    C("Estado", "Estado"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    N("SumCuota", "Σ cuota"),
                    C("CuotasPagadas", "Cuotas pag."),
                    N("MontoGastosAdm", "Gastos adm."),
                    N("SumInteres", "Σ interés"),
                    N("SumMora", "Σ mora"),
                    N("SumPago", "Σ pago"))),
            [CredixLegacyReportKey.CreditoAprobacion] = Def(
                "CRÉDITOS APROBADOS",
                Cols(
                    C("CreditoId", "N° créd."),
                    L("Oficina", "Oficina"),
                    L("Cliente", "Cliente"),
                    C("FechaAprobacion", "F. aprobación"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    C("NumeroCuotas", "N° cuotas"),
                    N("MontoDesembolso", "Desembolso"),
                    L("Gestor", "Gestor"))),
            [CredixLegacyReportKey.CreditosActivos] = Def(
                "CRÉDITOS ACTIVOS",
                CreditosActivosCols()),
            [CredixLegacyReportKey.CreditosCierres] = Def(
                "CRÉDITOS CIERRES",
                Cols(
                    C("CreditoId", "N° créd."),
                    C("Estado", "Estado"),
                    L("Agente", "Agente"),
                    C("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    C("FormaPago", "Forma pago"),
                    C("NumeroCuotas", "N° cuotas"),
                    N("Interes", "Interés"),
                    N("MontoGastosAdm", "Gastos adm."),
                    C("CentralRiesgo", "Cent. riesgo"),
                    C("FechaPrimerPago", "F. primer pago"),
                    C("FechaVencimiento", "F. vencimiento"),
                    N("SumAmortizacion", "Σ capital"),
                    N("SumInteres", "Σ interés"),
                    N("SumCuota", "Σ cuota"),
                    C("SumNroCuota", "Σ n° cuota"))),
            [CredixLegacyReportKey.CreditosMorososPagados] = Def(
                "CRÉDITOS MOROSOS PAGADOS",
                Cols(
                    C("CreditoId", "N° créd."),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    N("Interes", "Interés"),
                    C("FormaPago", "Forma pago"),
                    C("NumeroCuotas", "N° cuotas"),
                    N("MontoGastosAdm", "Gastos adm."),
                    C("CentralRiesgo", "Cent. riesgo"),
                    C("FechaPrimerPago", "F. primer pago"),
                    C("FechaVencimiento", "F. vencimiento"),
                    C("FechaPagado", "F. pagado"),
                    L("Agente", "Agente"))),
            [CredixLegacyReportKey.ReporteCredito] = Def(
                "REPORTE DE CRÉDITOS",
                Cols(
                    L("Producto", "Producto"),
                    L("Cliente", "Cliente"),
                    C("CreditoId", "N° créd."),
                    C("FechaDesembolso", "F. desembolso"),
                    C("FechaVcto", "F. vcto"),
                    C("FormaPago", "Forma pago"),
                    C("NumeroCuotas", "N° cuotas"),
                    N("Interes", "Interés"),
                    C("Estado", "Estado"),
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
                    C("CajaDiarioId", "N°"),
                    L("Oficina", "Oficina"),
                    L("Caja", "Caja"),
                    L("Agente", "Agente"),
                    N("SaldoInicial", "Saldo inicial"),
                    N("Entradas", "Entradas"),
                    N("Salidas", "Salidas"),
                    N("SaldoFinal", "Saldo final"),
                    C("FechaIniOperacion", "Inicio op."),
                    C("FechaFinOperacion", "Fin op."))),
            [CredixLegacyReportKey.CreditoVencido] = Def(
                "CRÉDITO VENCIDO",
                Cols(
                    L("Gestor", "Gestor"),
                    C("CreditoId", "N° créd."),
                    L("Cliente", "Cliente"),
                    N("MontoCredito", "Monto crédito"),
                    C("FormaPago", "Forma pago"),
                    C("FechaVencimiento", "F. vencimiento"),
                    N("CreditoVencido", "Vencido"),
                    N("VencidoMenor60", "< 60 días"),
                    N("VencidoMayor60", "> 60 días"),
                    N("VencidoIrrecuperable", "Irrecuperable"))),
            [CredixLegacyReportKey.MovimientoCajaAnulado] = Def(
                "MOVIMIENTOS CAJA ANULADOS",
                Cols(
                    C("MovimientoCajaId", "N°"),
                    L("Operacion", "Operación"),
                    N("ImportePago", "Importe"),
                    L("Persona", "Persona"),
                    L("Descripcion", "Descripción"),
                    C("FechaReg", "F. registro"),
                    L("UsuarioRegistro", "Usuario reg."),
                    L("MotivoAnulacion", "Motivo"),
                    C("FechaAnulacion", "F. anulación"),
                    L("UsuarioAnulacion", "Usuario anul."))),
            [CredixLegacyReportKey.SaldoCarteraCajaDiario] = Def(
                "SALDO CARTERA Y CAJA DIARIO",
                SaldoCarteraCols()),
            [CredixLegacyReportKey.ComprobantesCajaChica] = Def(
                "COMPROBANTES CAJA CHICA",
                Cols(
                    L("Gasto", "Gasto"),
                    C("Fecha", "Fecha"),
                    C("Documento", "Documento"),
                    C("Serie", "Serie"),
                    C("Numero", "Número"),
                    C("RUC", "RUC"),
                    L("RazonSocial", "Razón social"),
                    L("DetalleGasto", "Detalle"),
                    N("Importe", "Importe"))),
            [CredixLegacyReportKey.CajasAsignadas] = Def(
                "CAJAS ASIGNADAS",
                Cols(
                    C("CajaDiarioId", "N°"),
                    L("Caja", "Caja"),
                    C("Modo", "Modo"),
                    L("Cajero", "Cajero"),
                    C("FechaIniOperacion", "Inicio"),
                    C("FechaFinOperacion", "Fin"),
                    // Orden del RDLC legacy (entradas antes de salidas), no el del SP.
                    N("SaldoInicial", "Saldo ini."),
                    N("Entradas", "Entradas"),
                    N("Salidas", "Salidas"),
                    N("SaldoFinal", "Saldo final"),
                    L("Resumen", "Resumen")),
                totals: ["SaldoInicial", "Entradas", "Salidas", "SaldoFinal"]),
            [CredixLegacyReportKey.CobroDiarioDetalle] = Def(
                "COBRO DIARIO DETALLE",
                CobranzaDetalleCols()),
            [CredixLegacyReportKey.CobranzaPagos] = Def(
                "REPORTE DE COBRANZA POR GESTOR",
                CobranzaDetalleCols()),
            [CredixLegacyReportKey.MovimientoCredito] = Def(
                "MOVIMIENTO CRÉDITO",
                Cols(
                    C("Fecha", "Fecha"),
                    L("Operacion", "Operación"),
                    L("Glosa", "Glosa"),
                    N("ImportePago", "Importe"),
                    N("Saldo", "Saldo")),
                landscape: false),
            [CredixLegacyReportKey.ListarSaldoCartera] = Def(
                "SALDO CARTERA",
                Cols(
                    C("AgenteId", "N° agente"),
                    C("OficinaId", "N° oficina"),
                    C("NroDesembolsos", "N° desembolsos"),
                    N("MontoDesembolsos", "Monto desembolsos"),
                    N("SaldoCartera", "Saldo cartera"),
                    C("NroClientesSaldoCartera", "Cli. cartera"),
                    N("SaldoMoraCartera", "Saldo mora"),
                    C("NroClientesSaldoMoraCartera", "Cli. mora"),
                    N("SaldoVencido", "Saldo vencido"),
                    N("SaldoMorosidad", "Saldo morosidad"),
                    C("NroClientesNuevos", "Cli. nuevos"),
                    C("FechaCierre", "F. cierre"))),
            [CredixLegacyReportKey.AvalPersona] = Def(
                "AVAL PERSONA",
                Cols(
                    L("Grupo", "Grupo"),
                    C("CreditoId", "N° créd."),
                    N("MontoCredito", "Monto"),
                    C("Estado", "Estado"),
                    L("Persona", "Persona"),
                    C("Dni", "DNI"),
                    C("Celular", "Celular"))),
            [CredixLegacyReportKey.SaldosCaja] = Def(
                "SALDOS CAJA",
                Cols(
                    C("FechaReg", "Fecha"),
                    L("Operacion", "Operación"),
                    C("Codigo", "Código"),
                    L("Cliente", "Cliente"),
                    N("ImportePago", "Importe"),
                    C("IndEntrada", "Entrada"),
                    L("Glosa", "Glosa"),
                    C("TipoPago", "Tipo pago")),
                totals: ["ImportePago"]),
            [CredixLegacyReportKey.MovimientoBoveda] = Def(
                "MOVIMIENTO BÓVEDA",
                Cols(
                    C("FechaReg", "Fecha"),
                    C("CodOperacion", "Cód. op."),
                    L("Glosa", "Glosa"),
                    N("Entrada", "Entrada"),
                    N("Salida", "Salida"),
                    C("TipoPago", "Tipo pago"),
                    L("Agente", "Agente")),
                totals: ["Entrada", "Salida"]),
            [CredixLegacyReportKey.CreditoMorosidad] = Def(
                "MOROSIDAD DE CRÉDITOS",
                Cols(
                    C("CreditoId", "N° créd."),
                    L("Cliente", "Cliente"),
                    L("Direccion", "Dirección"),
                    C("Celular", "Celular"),
                    C("FechaDesembolso", "F. desembolso"),
                    C("FechaVcto", "F. vcto"),
                    L("Articulo", "Artículo"),
                    N("MontoCredito", "Monto crédito"),
                    N("SaldoCredito", "Saldo"),
                    C("FechaUltPago", "Últ. pago"),
                    N("CapitalAtrazo", "Cap. atraso"),
                    N("GA", "GA"),
                    N("InteresAtrazo", "Int. atraso"),
                    N("Mora", "Mora"),
                    N("ImporteLibre", "Imp. libre"),
                    C("DiasAtrazo", "Días"),
                    C("CuotasAtrazo", "Cuotas"),
                    N("DeudaAtrazo", "Deuda atraso"))),
            [CredixLegacyReportKey.CentralRiesgoGenerar] = Def(
                "CENTRAL DE RIESGO",
                Cols(
                    C("Anio", "Año"),
                    C("Mes", "Mes"),
                    C("CreditoId", "N° créd."),
                    C("Periodo", "Periodo"),
                    L("Entidad", "Entidad"),
                    C("TipoDoc", "Tipo doc."),
                    C("NumDoc", "N° doc."),
                    L("RazonSocial", "Razón social"),
                    L("ApePat", "Ap. paterno"),
                    L("ApeMat", "Ap. materno"),
                    L("Nombres", "Nombres"),
                    C("TipoPersona", "Tipo persona"),
                    C("ModalidadCredito", "Modalidad"),
                    N("DeudaMenor30", "Deuda < 30"),
                    N("DeudaMayor30", "Deuda > 30"),
                    C("Calificacion", "Calificación"),
                    C("DiasAtrazo", "Días atraso"),
                    L("Direccion", "Dirección"),
                    C("celular", "Celular"))),
            [CredixLegacyReportKey.PagosNoVerificados] = Def(
                "PAGOS NO VERIFICADOS",
                Cols(
                    L("Cliente", "Cliente"),
                    L("Movimiento", "Movimiento"),
                    N("ImportePago", "Importe"),
                    C("TipoPago", "Tipo pago"),
                    C("FechaTransferencia", "F. transferencia"),
                    L("Registro", "Registro"))),
            [CredixLegacyReportKey.ReporteStock] = Def(
                "REPORTE STOCK",
                Cols(
                    C("Nro", "Nro"),
                    C("TipoArticulo", "Tipo"),
                    L("Articulo", "Artículo"),
                    C("Stock", "Stock"),
                    L("Series", "Series"))),
            [CredixLegacyReportKey.StockAnulados] = Def(
                "STOCK ANULADOS",
                Cols(
                    L("Movimiento", "Movimiento"),
                    L("Observacion", "Observación"),
                    C("Fecha", "Fecha"),
                    C("Cantidad", "Cantidad"),
                    L("Detalle", "Detalle"))),
            [CredixLegacyReportKey.ListaPrecio] = Def(
                "LISTA DE PRECIOS",
                Cols(
                    C("TipoArticulo", "Tipo"),
                    L("ArticuloDes", "Artículo"),
                    N("Monto", "Monto"),
                    N("Descuento", "Descuento"),
                    C("PuntosCanje", "Puntos")),
                landscape: false),
            [CredixLegacyReportKey.Kardex] = Def(
                "KARDEX",
                Cols(
                    C("Fecha", "Fecha"),
                    L("Concepto", "Concepto"),
                    C("CantEnt", "Cant. ent."),
                    N("PUEnt", "PU ent."),
                    N("TotalEnt", "Total ent."),
                    C("CantSal", "Cant. sal."),
                    N("PUSal", "PU sal."),
                    N("TotalSal", "Total sal."),
                    C("CantSaldo", "Cant. saldo"),
                    N("PUSaldo", "PU saldo"),
                    N("TotalSaldo", "Total saldo")),
                landscape: false),
            [CredixLegacyReportKey.RentabilidadVenta] = Def(
                "RENTABILIDAD VENTA",
                Cols(
                    C("Nro", "Nro"),
                    C("Codigo", "Código"),
                    L("Articulo", "Artículo"),
                    C("FechaEnt", "F. entrada"),
                    N("PrecioEnt", "P. entrada"),
                    C("FechaSal", "F. salida"),
                    N("PrecioSal", "P. salida"),
                    C("Modalidad", "Modalidad"),
                    N("Rentabilidad", "Rentabilidad"),
                    L("Cliente", "Cliente"))),
        };

    private static CredixLegacyColumnSpec[] CobranzaDetalleCols() =>
        Cols(
            C("Nro", "Nro"),
            L("Cliente", "Cliente"),
            C("FormaPago", "Tipo"),
            N("MontoCredito", "Crédito"),
            N("Interes", "Interés"),
            N("MontoTotal", "Monto total"),
            C("FechaPrimerPago", "1er pago"),
            C("FechaVencimiento", "Vencimiento"),
            N("TotalPago", "Total pagado"),
            N("Saldo", "Saldo"),
            C("DiasAtrazoMora", "Días mora"),
            L("Pagos", "Pagos"));

    private static CredixLegacyColumnSpec[] CobroDiarioCols() =>
        Cols(
            C("Nro", "Nro"),
            C("Orden", "Ord."),
            C("CreditoId", "N° créd."),
            L("Cliente", "Cliente"),
            C("Celular", "Celular"),
            N("MontoCredito", "Monto crédito"),
            N("Interes", "Interés"),
            N("CuotaPlan", "Cuota plan"),
            N("Saldo", "Saldo"),
            C("DiasAtrazo", "Días atraso"),
            C("NroCuotasPen", "Cuotas pend."),
            N("CuotaTotal", "Cuota total"),
            L("Direccion", "Dirección"),
            C("FechaPago", "F. pago"),
            C("FechaPrimerPago", "F. 1er pago"),
            C("FechaVencimiento", "F. vcto"),
            N("Mora", "Mora"),
            N("MontoTotal", "Monto total"),
            L("Negocio", "Negocio"),
            C("FormaPago", "Forma pago"),
            N("TopeCredito", "Tope crédito"),
            C("ClasificacionRiesgoSBS", "Clasif. SBS"));

    private static CredixLegacyColumnSpec[] ObservadoCols() =>
        Cols(
            L("Oficina", "Oficina"),
            C("CreditoId", "N° créd."),
            L("Cliente", "Cliente"),
            C("FechaPrimerPago", "F. 1er pago"),
            C("FechaVencimiento", "F. vcto"),
            N("MontoCredito", "Monto crédito"),
            N("Interes", "Interés"),
            L("Agente", "Agente"),
            L("Observacion", "Observación"),
            N("TramiteAdm", "Trámite adm."),
            C("CentralRiesgo", "Cent. riesgo"));

    private static CredixLegacyColumnSpec[] ClientesNuevosCols() =>
        ObservadoCols();

    private static CredixLegacyColumnSpec[] CreditosActivosCols() =>
        Cols(
            C("Nro", "Nro"),
            C("Estado", "Estado"),
            L("Agente", "Agente"),
            C("CreditoId", "N° créd."),
            C("Codigo", "Código"),
            L("Cliente", "Cliente"),
            N("MontoCredito", "Monto crédito"),
            C("FormaPago", "Forma pago"),
            C("NumeroCuotas", "N° cuotas"),
            N("Interes", "Interés"),
            N("MontoInteres", "Monto int."),
            N("MontoCreditoTotal", "Total crédito"),
            N("MontoGastosAdm", "Gastos adm."),
            C("CentralRiesgo", "Cent. riesgo"),
            C("FechaPrimerPago", "F. 1er pago"),
            C("FechaVencimiento", "F. vcto"),
            C("NroCuotasPagado", "Cuotas pag."),
            N("Pagado", "Pagado"),
            N("InteresPagado", "Int. pagado"),
            C("NroCuotasPen", "Cuotas pend."),
            N("SaldoCapital", "Saldo cap."),
            N("SaldoInteres", "Saldo int."),
            N("Saldo", "Saldo"),
            C("DiasAtrazo", "Días atraso"),
            N("Mora", "Mora"));

    private static CredixLegacyColumnSpec[] SaldoCarteraCols() =>
        Cols(
            L("Oficina", "Oficina"),
            L("Caja", "Caja"),
            L("Agente", "Agente"),
            C("FechaCierreIni", "Cierre\nini."),
            N("SalidasIni", "Desemb.\nini."),
            N("MontoCobradoIni", "Cobrado\nini."),
            N("PocentajeCobroIni", "% cobro\nini."),
            N("SaldoCarteraSinMoraIni", "Cart. s/mora\nini."),
            N("NroClientesCarteraSinMoraIni", "Cli. s/mora\nini."),
            N("SaldoMoraCarteraIni", "Mora\nini."),
            N("NroClientesSaldoMoraCarteraIni", "Cli. mora\nini."),
            N("NroClientesNuevosIni", "Nuevos\nini."),
            N("SaldoVencidoIni", "Vencido\nini."),
            N("SaldoMorosidadIni", "Morosid.\nini."),
            C("FechaCierreFin", "Cierre\nfin"),
            N("SalidasFin", "Desemb.\nfin"),
            N("MontoCobradoFin", "Cobrado\nfin"),
            N("PocentajeCobroFin", "% cobro\nfin"),
            N("SaldoCarteraSinMoraFin", "Cart. s/mora\nfin"),
            N("NroClientesCarteraSinMoraFin", "Cli. s/mora\nfin"),
            N("SaldoMoraCarteraFin", "Mora\nfin"),
            N("NroClientesSaldoMoraCarteraFin", "Cli. mora\nfin"),
            N("NroClientesNuevosFin", "Nuevos\nfin"),
            N("SaldoVencidoFin", "Vencido\nfin"),
            N("SaldoMorosidadFin", "Morosid.\nfin"));

    private static CredixLegacyReportDefinition Def(
        string title,
        CredixLegacyColumnSpec[] columns,
        bool landscape = true,
        string[]? totals = null) =>
        new(title, columns, landscape, totals);

    private static CredixLegacyColumnSpec[] Cols(params CredixLegacyColumnSpec[] cols) => cols;

    private static CredixLegacyColumnSpec L(string csv, string label) =>
        new(csv, label, CredixColumnAlign.Left, CredixColumnWeights.For(csv, CredixColumnAlign.Left));

    private static CredixLegacyColumnSpec C(string csv, string label) =>
        new(csv, label, CredixColumnAlign.Center, CredixColumnWeights.For(csv, CredixColumnAlign.Center));

    private static CredixLegacyColumnSpec N(string csv, string label) =>
        new(csv, label, CredixColumnAlign.Right, CredixColumnWeights.For(csv, CredixColumnAlign.Right));
}

/// <summary>Definición de layout PDF para un informe.</summary>
public sealed class CredixLegacyReportDefinition
{
    public CredixLegacyReportDefinition(
        string title,
        IReadOnlyList<CredixLegacyColumnSpec> columns,
        bool landscape = true,
        IReadOnlyList<string>? totalColumns = null)
    {
        Title = title;
        Columns = columns;
        Landscape = landscape;
        TotalColumns = totalColumns ?? Array.Empty<string>();
    }

    public string Title { get; }
    public IReadOnlyList<CredixLegacyColumnSpec> Columns { get; }
    public bool Landscape { get; }

    /// <summary>Columnas (nombre CSV) que se suman en la fila «TOTAL» del PDF.</summary>
    public IReadOnlyList<string> TotalColumns { get; }
}
