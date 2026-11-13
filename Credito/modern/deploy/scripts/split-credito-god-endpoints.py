#!/usr/bin/env python3
"""Mechanical split of credito god endpoint files into domain modules + facade.

Preserves route registration order via facade. No logic changes.
"""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Credito.Modern.Api" / "credito"


def find_map_starts(lines: list[str]) -> list[int]:
    """1-based line numbers where app.Map(Get|Post|Delete) starts."""
    starts: list[int] = []
    pat = re.compile(r"^\s*app\.Map(Get|Post|Delete)\s*\(")
    for i, line in enumerate(lines, start=1):
        if pat.match(line):
            starts.append(i)
    return starts


def slice_endpoints(
    lines: list[str], starts: list[int], first_1: int, last_1: int
) -> str:
    """Extract inclusive endpoint bodies from first_1..last_1 Map start lines (1-based).

    last_1 is the start line of the last endpoint to include; body runs until
    the line before the next Map start (or before closing brace of Map method).
    """
    try:
        i0 = starts.index(first_1)
        i1 = starts.index(last_1)
    except ValueError as ex:
        raise SystemExit(f"Map start not found: {ex}") from ex

    begin = starts[i0] - 1  # 0-based
    if i1 + 1 < len(starts):
        end = starts[i1 + 1] - 1  # exclusive: line before next map
    else:
        # Last endpoint in file: strip trailing blank lines and the method/class closers.
        # Find last non-empty content line that is not a lone closing brace of the outer method.
        end = len(lines)
        # Walk back past closing braces of Map* method and class (typically 2–3 '}' at end).
        while end > begin and lines[end - 1].strip() in ("", "}", "{"):
            # Keep endpoint's own closing if we're still inside; stop when we hit
            # blank+}} pattern at file end. Safer: find last line matching .Produces or );
            end -= 1
        # Re-advance to include the last endpoint's fluent chain ending (usually ends with ;)
        # Actually we over-stripped. Better approach below.
        end = len(lines)
        # Remove final closing braces of Map method + class + trailing blanks.
        while end > 0 and lines[end - 1].strip() == "":
            end -= 1
        # Expect: "}" (class), "}" (method) — strip two closing braces if present
        stripped = 0
        while end > 0 and stripped < 2 and lines[end - 1].strip() == "}":
            end -= 1
            stripped += 1
        while end > 0 and lines[end - 1].strip() == "":
            end -= 1

    chunk = "".join(lines[begin:end])
    # Normalize trailing whitespace
    return chunk.rstrip() + "\n"


def extract_usings(lines: list[str]) -> str:
    usings: list[str] = []
    for line in lines:
        if line.startswith("using "):
            usings.append(line.rstrip("\n"))
        elif line.startswith("namespace "):
            break
    return "\n".join(usings) + "\n"


def write_domain_file(
    path: Path,
    class_name: str,
    map_method: str,
    usings: str,
    body: str,
    summary: str,
) -> None:
    content = (
        f"{usings}\n"
        f"namespace Credito.Modern.Api.Credito;\n\n"
        f"/// <summary>{summary}</summary>\n"
        f"internal static class {class_name}\n"
        f"{{\n"
        f"    public static void {map_method}(this WebApplication app)\n"
        f"    {{\n"
        f"{indent_block(body, 8)}"
        f"    }}\n"
        f"}}\n"
    )
    path.write_text(content, encoding="utf-8", newline="\n")
    print(f"Wrote {path.name} ({len(content.splitlines())} lines)")


def indent_block(text: str, spaces: int) -> str:
    pad = " " * spaces
    out: list[str] = []
    for line in text.splitlines(True):
        if line.strip() == "":
            out.append("\n")
        else:
            # Original endpoint bodies are indented with 8 spaces inside Map method.
            # When extracting from god file they already have 8 spaces; we need them
            # inside the new Map method also at 8 spaces — keep as-is if already indented,
            # else add pad.
            if line.startswith("        "):
                out.append(line)
            else:
                out.append(pad + line.lstrip() if line.strip() else line)
    return "".join(out)


def write_facade(
    path: Path, class_name: str, map_method: str, delegates: list[str], summary: str
) -> None:
    calls = "\n".join(f"        app.{d}();" for d in delegates)
    content = (
        "namespace Credito.Modern.Api.Credito;\n\n"
        f"/// <summary>{summary}</summary>\n"
        f"internal static class {class_name}\n"
        f"{{\n"
        f"    public static void {map_method}(this WebApplication app)\n"
        f"    {{\n"
        f"{calls}\n"
        f"    }}\n"
        f"}}\n"
    )
    path.write_text(content, encoding="utf-8", newline="\n")
    print(f"Wrote facade {path.name}")


def concat_ranges(
    lines: list[str], starts: list[int], ranges: list[tuple[int, int]]
) -> str:
    parts: list[str] = []
    for a, b in ranges:
        parts.append(slice_endpoints(lines, starts, a, b))
    return "\n".join(p.rstrip() + "\n" for p in parts)


def split_reportes() -> None:
    path = ROOT / "creditoreportesrestantesendpoints.cs"
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(True)
    starts = find_map_starts(lines)
    print(f"reportes: {len(starts)} Map* starts")
    usings = extract_usings(lines)

    # (filename, class, map_method, summary, ranges of (first_map_line, last_map_line))
    # Preserve registration order in facade matching original file order as much as possible.
    # Central riesgo is non-contiguous: TXT early + JSON/CSV/PDF later — keep in one file;
    # facade will call that file once at the position of the first block, then continue.
    # To preserve exact order we need either:
    #  A) two Map calls from same file (early + late methods), or
    #  B) accept reordering central-riesgo-json next to TXT (safe: distinct routes).
    # Choose B for simplicity (all central riesgo together), then remaining in file order.

    # 86 MapGet routes (indices verified by _list-map-routes.py)
    specs: list[tuple[str, str, str, str, list[tuple[int, int]]]] = [
        (
            "creditoreportecentralriesgosaldocarteraendpoints.cs",
            "CreditoReporteCentralRiesgoSaldoCarteraEndpoints",
            "MapCreditoReporteCentralRiesgoSaldoCarteraEndpoints",
            "Central de riesgo + listar saldo cartera (JSON/CSV/PDF/TXT).",
            [(starts[0], starts[0]), (starts[1], starts[3]), (starts[35], starts[37])],
        ),
        (
            "creditoreportereajustesendpoints.cs",
            "CreditoReporteReajustesEndpoints",
            "MapCreditoReporteReajustesEndpoints",
            "Movimiento crédito y cajas asignadas.",
            [(starts[4], starts[9])],
        ),
        (
            "creditoreportecobrodiarioendpoints.cs",
            "CreditoReporteCobroDiarioEndpoints",
            "MapCreditoReporteCobroDiarioEndpoints",
            "Cobro diario y cobro diario detalle.",
            [(starts[10], starts[15])],
        ),
        (
            "creditoreporteclientesendpoints.cs",
            "CreditoReporteClientesEndpoints",
            "MapCreditoReporteClientesEndpoints",
            "Clientes bloqueados, tope crédito, inactivos y nuevos del mes.",
            [
                (starts[16], starts[21]),
                (starts[59], starts[61]),
                (starts[83], starts[85]),
            ],
        ),
        (
            "creditoreportecajabovedaendpoints.cs",
            "CreditoReporteCajaBovedaEndpoints",
            "MapCreditoReporteCajaBovedaEndpoints",
            "Aval, saldos caja, movimiento bóveda, resumen cuenta y métricas vencimiento.",
            [(starts[22], starts[34])],
        ),
        (
            "creditoreportecreditocarteraendpoints.cs",
            "CreditoReporteCreditoCarteraEndpoints",
            "MapCreditoReporteCreditoCarteraEndpoints",
            "Cartera crédito: estado, morosidad, rentabilidad, aprobación, activos, cierres, morosos pagados.",
            [(starts[38], starts[58])],
        ),
        (
            "creditoreportecajaoperacionendpoints.cs",
            "CreditoReporteCajaOperacionEndpoints",
            "MapCreditoReporteCajaOperacionEndpoints",
            "Caja diario, crédito vencido, notificaciones, movimientos anulados y saldo cartera caja.",
            [(starts[62], starts[74])],
        ),
        (
            "creditoreportecondonacionpagosendpoints.cs",
            "CreditoReporteCondonacionPagosEndpoints",
            "MapCreditoReporteCondonacionPagosEndpoints",
            "Pagos no verificados, crédito observado y condonado.",
            [(starts[75], starts[82])],
        ),
    ]

    # Verify we cover all starts exactly once
    covered: set[int] = set()
    for _, _, _, _, ranges in specs:
        for a, b in ranges:
            ia, ib = starts.index(a), starts.index(b)
            for s in starts[ia : ib + 1]:
                if s in covered:
                    raise SystemExit(f"Duplicate coverage of map start {s}")
                covered.add(s)
    missing = set(starts) - covered
    if missing:
        raise SystemExit(f"Uncovered map starts: {sorted(missing)}")

    for fname, cls, method, summary, ranges in specs:
        body = concat_ranges(lines, starts, ranges)
        write_domain_file(ROOT / fname, cls, method, usings, body, summary)

    write_facade(
        path,
        "CreditoReportesRestantesEndpoints",
        "MapCreditoReportesRestantesEndpoints",
        [s[2] for s in specs],
        "Fachada: reportes crédito restantes partidos por dominio (sin cambiar rutas).",
    )


def split_operacion() -> None:
    path = ROOT / "creditooperacionendpoints.cs"
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(True)
    starts = find_map_starts(lines)
    print(f"operacion: {len(starts)} Map* starts")
    usings = extract_usings(lines)

    # Index-based clusters from explore agent (0-based into starts[]):
    # A+B: 0..9 (simulador + plan/mora) — 10 routes
    # C: caja diario lecturas — need to find by route
    # Better: resolve by route substring for robustness

    def start_for(substr: str) -> int:
        for s in starts:
            # Look ahead a few lines for the route string
            window = "".join(lines[s - 1 : s + 3])
            if substr in window:
                return s
        raise SystemExit(f"Route containing {substr!r} not found")

    # Anchor routes for clusters
    a0 = start_for("/calcular-tem")
    b_last = start_for("/obtener-monto-pendiente-plan-pago")
    c0 = start_for("/usuarios-no-asignados-caja")
    c_last = start_for("/caja-diario-sesion")
    d0 = start_for("/credito-contexto")
    d_last = start_for("/creditos-por-persona")
    e0 = start_for("/pagos-no-verificados")
    e_last = start_for("/pagar-cuotas-cancelacion")
    f0 = start_for("/validar-cierre-caja-diario")
    f_last = start_for("/pagar-cuenta-por-cobrar")
    g0 = None
    for s in starts:
        window = "".join(lines[s - 1 : s + 3])
        if "/api/v1/credito/boveda" in window or "/api/v1/credito/existe-boveda" in window:
            g0 = s
            break
    if g0 is None:
        raise SystemExit("First boveda route not found")
    j0 = start_for("/caja-chica-sesion")
    # G+H+I ends just before J
    i_last = starts[starts.index(j0) - 1]
    k0 = start_for("/validar-anular-credito")
    j_last = starts[starts.index(k0) - 1]
    k_last = start_for("/rechazar-credito")
    l0 = start_for("/cerrar-cajas-diarios")

    specs: list[tuple[str, str, str, str, list[tuple[int, int]]]] = [
        (
            "creditosimuladorcarteraendpoints.cs",
            "CreditoSimuladorCarteraEndpoints",
            "MapCreditoSimuladorCarteraEndpoints",
            "Simulador, TEM/gastos y lectura de plan/mora.",
            [(a0, b_last)],
        ),
        (
            "creditocajadiarioendpoints.cs",
            "CreditoCajaDiarioEndpoints",
            "MapCreditoCajaDiarioEndpoints",
            "Sesión, cierre, desembolso, movimientos y CxC de caja diario.",
            [(c0, c_last), (f0, f_last), (l0, l0)],
        ),
        (
            "creditofichagestionendpoints.cs",
            "CreditoFichaGestionEndpoints",
            "MapCreditoFichaGestionEndpoints",
            "Ficha crédito: contexto, evidencias, prendas, avales y gestiones.",
            [(d0, d_last)],
        ),
        (
            "creditocobrocuotasendpoints.cs",
            "CreditoCobroCuotasEndpoints",
            "MapCreditoCobroCuotasEndpoints",
            "Cobro de cuotas, verificación de transferencias e impagos.",
            [(e0, e_last)],
        ),
        (
            "creditobovedaoperacionendpoints.cs",
            "CreditoBovedaOperacionEndpoints",
            "MapCreditoBovedaOperacionEndpoints",
            "Bóveda: consultas, cierres, asignación de caja y transferencias.",
            [(g0, i_last)],
        ),
        (
            "creditocajachicaoperacionendpoints.cs",
            "CreditoCajaChicaOperacionEndpoints",
            "MapCreditoCajaChicaOperacionEndpoints",
            "Caja chica: sesión, movimientos, rendiciones y cierres.",
            [(j0, j_last)],
        ),
        (
            "creditocicloendpoints.cs",
            "CreditoCicloEndpoints",
            "MapCreditoCicloEndpoints",
            "Ciclo de vida: aprobar, anular, reprogramar, prorrogar, crear y rechazar.",
            [(k0, k_last)],
        ),
    ]

    # Facade order must match original as closely as practical:
    # A+B, C, D, E, F, G+H+I, J, K, L — but L is inside caja diario file.
    # Call order: simulador, caja(lecturas+ops+L), ficha, cobro, boveda, chica, ciclo
    # Original order: A B C D E F G H I J K L
    # Our call order moves L earlier (with F) — distinct route, OK.
    # But C is called before D in original; we call caja (C+F+L) before ficha (D),
    # which registers F before D — route order change vs original.
    # Safer facade order to minimize drift:
    #   simulador (A+B)
    #   then we need C alone then D then E then F then ...
    # So split C into its own tiny file OR accept reordering.
    # Prefer exact-ish order: keep C in caja file but add MapCreditoCajaDiarioSesionEndpoints
    # and MapCreditoCajaDiarioOperacionEndpoints — exceeds simplicity.
    #
    # Practical: two methods in caja file:
    #   MapCreditoCajaDiarioSesionEndpoints (C)
    #   MapCreditoCajaDiarioOperacionEndpoints (F+L)
    # Facade: sim, sesion(C), ficha, cobro, operacion(F+L), boveda, chica, ciclo

    # Rebuild caja as two map methods in one file for order preservation.
    sesion_body = concat_ranges(lines, starts, [(c0, c_last)])
    ops_body = concat_ranges(lines, starts, [(f0, f_last), (l0, l0)])
    caja_path = ROOT / "creditocajadiarioendpoints.cs"
    caja_content = (
        f"{usings}\n"
        "namespace Credito.Modern.Api.Credito;\n\n"
        "/// <summary>Sesión, cierre, desembolso, movimientos y CxC de caja diario.</summary>\n"
        "internal static class CreditoCajaDiarioEndpoints\n"
        "{\n"
        "    public static void MapCreditoCajaDiarioSesionEndpoints(this WebApplication app)\n"
        "    {\n"
        f"{indent_block(sesion_body, 8)}"
        "    }\n\n"
        "    public static void MapCreditoCajaDiarioOperacionEndpoints(this WebApplication app)\n"
        "    {\n"
        f"{indent_block(ops_body, 8)}"
        "    }\n"
        "}\n"
    )
    caja_path.write_text(caja_content, encoding="utf-8", newline="\n")
    print(f"Wrote {caja_path.name} ({len(caja_content.splitlines())} lines)")

    # Other specs excluding the caja one we already wrote
    other = [s for s in specs if s[0] != "creditocajadiarioendpoints.cs"]
    covered: set[int] = set()
    for _, _, _, _, ranges in other:
        for a, b in ranges:
            ia, ib = starts.index(a), starts.index(b)
            for s in starts[ia : ib + 1]:
                covered.add(s)
    for a, b in [(c0, c_last), (f0, f_last), (l0, l0)]:
        ia, ib = starts.index(a), starts.index(b)
        for s in starts[ia : ib + 1]:
            covered.add(s)
    missing = set(starts) - covered
    if missing:
        raise SystemExit(
            f"Uncovered operacion map starts: {sorted(missing)} "
            f"routes near {[lines[s-1:s+2] for s in sorted(missing)[:3]]}"
        )
    extra = covered - set(starts)
    if extra:
        raise SystemExit(f"Extra coverage: {extra}")

    for fname, cls, method, summary, ranges in other:
        body = concat_ranges(lines, starts, ranges)
        write_domain_file(ROOT / fname, cls, method, usings, body, summary)

    write_facade(
        path,
        "CreditoOperacionEndpoints",
        "MapCreditoOperacionEndpoints",
        [
            "MapCreditoSimuladorCarteraEndpoints",
            "MapCreditoCajaDiarioSesionEndpoints",
            "MapCreditoFichaGestionEndpoints",
            "MapCreditoCobroCuotasEndpoints",
            "MapCreditoCajaDiarioOperacionEndpoints",
            "MapCreditoBovedaOperacionEndpoints",
            "MapCreditoCajaChicaOperacionEndpoints",
            "MapCreditoCicloEndpoints",
        ],
        "Fachada: operación crédito partida por dominio (sin cambiar rutas).",
    )


def main() -> None:
    split_reportes()
    split_operacion()
    print("Done.")


if __name__ == "__main__":
    main()
