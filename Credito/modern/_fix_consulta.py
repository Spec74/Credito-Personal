from pathlib import Path

p = Path(r"D:\GitHub\Credito\modern\Credito.Modern.Web\src\pages\credito\ConsultaCreditoPage.tsx")
lines = p.read_text(encoding="utf-8").splitlines(keepends=True)

# Keep lines 1-369 (index 0:369), skip corruption, keep from line 817 (index 816)
head = lines[:369]
tail = lines[816:]

middle = """  const moraResumenQuery = useQuery({
    queryKey: ['credito-mora-resumen', activeId],
    queryFn: () => fetchCreditoMoraResumen(activeId!),
    enabled: activeId != null && activeId > 0,
  })

  const pendientes =
    planQuery.data?.filter((c) => c.estado !== 'PAG').length ?? 0

  const moraVigente = moraQuery.data?.moraPendiente ?? 0
  const moraPostergada = moraResumenQuery.data?.saldoPostergado ?? 0
  const moraTotal =
    moraResumenQuery.data?.indMoraProducto === true
      ? moraVigente + moraPostergada
      : moraVigente

  const creditoStats: CredixStatItem[] =
    activeId != null
      ? [
          { value: activeId, label: 'Crédito N°' },
          {
            value:
              moraQuery.isLoading || moraResumenQuery.isLoading
                ? '…'
                : formatMoney(moraTotal),
            label: 'Mora total (S/.)',
            tone: 'red',
          },
          {
            value:
              moraResumenQuery.isLoading
                ? '…'
                : formatMoney(moraPostergada),
            label: 'Mora postergada (S/.)',
            tone: moraPostergada > 0 ? 'red' : undefined,
          },
          {
            value: planQuery.isLoading ? '…' : pendientes,
            label: 'Cuotas no pagadas',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.creditoVencido),
            label: 'Vencido total (S/.)',
            tone: 'red',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.vencidoMenor60),
            label: 'Venc. menor 60 d. (S/.)',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.vencidoMayor60),
            label: 'Venc. mayor 60 d. (S/.)',
            tone: 'red',
          },
        ]
      : []

"""

text = "".join(head)
text = text.replace(
    "    void queryClient.invalidateQueries({ queryKey: ['mora-pendiente', activeId] })\n"
    "    void queryClient.invalidateQueries({\n"
    "      queryKey: ['metricas-vencimiento-credito', activeId],\n"
    "    })",
    "    void queryClient.invalidateQueries({ queryKey: ['mora-pendiente', activeId] })\n"
    "    void queryClient.invalidateQueries({ queryKey: ['credito-mora-resumen', activeId] })\n"
    "    void queryClient.invalidateQueries({ queryKey: ['credito-mora-historial', activeId] })\n"
    "    void queryClient.invalidateQueries({\n"
    "      queryKey: ['metricas-vencimiento-credito', activeId],\n"
    "    })",
)

tail_text = "".join(tail)
tail_text = tail_text.replace(
    """              <Button
                icon={<ReloadOutlined />}
                onClick={() => setModalReprogramar(true)}
                disabled={oficinaId < 1}
              >
                Reprogramar
              </Button>
            </>
          )}
        </Space>
      </CredixFilterBar>

      {activeId != null && (""",
    """              <Button
                icon={<ReloadOutlined />}
                onClick={() => setModalReprogramar(true)}
                disabled={oficinaId < 1}
              >
                Reprogramar
              </Button>
              <Button
                icon={<HistoryOutlined />}
                onClick={() => setMoraModalOpen(true)}
              >
                Crédito mora
              </Button>
            </>
          )}
        </Space>
      </CredixFilterBar>

      {moraResumenQuery.data?.indMoraProducto &&
      (moraResumenQuery.data.saldoPostergado ?? 0) > 0 ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Mora postergada acumulada: ${formatMoney(moraResumenQuery.data.saldoPostergado)}`}
          description="Se liquidará al cobrar la última cuota pendiente (tabla CreditoMora)."
          action={
            <Button size="small" onClick={() => setMoraModalOpen(true)}>
              Ver historial
            </Button>
          }
        />
      ) : null}

      {activeId != null && (""",
)

tail_text = tail_text.replace(
    "      </Modal>\n    </CredixPage>",
    """      </Modal>

      <CreditoMoraModal
        open={moraModalOpen}
        creditoId={activeId}
        onClose={() => setMoraModalOpen(false)}
      />
    </CredixPage>""",
)

p.write_text(text + middle + tail_text, encoding="utf-8")
print("OK", p.stat().st_size)
