(function ($, window, document, undefined) {
    document.onselectstart = new Function("return false");
    //document.ondragstart = new Function("return false");

    $.datepicker.regional['es'] = {
        closeText: 'Cerrar',
        prevText: '&#x3c;Ant',
        nextText: 'Sig&#x3e;',
        currentText: 'Hoy',
        monthNames: ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
		'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'],
        monthNamesShort: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun',
		'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'],
        dayNames: ['Domingo', 'Lunes', 'Martes', 'Mi&eacute;rcoles', 'Jueves', 'Viernes', 'S&aacute;bado'],
        dayNamesShort: ['Dom', 'Lun', 'Mar', 'Mi&eacute;', 'Juv', 'Vie', 'S&aacute;b'],
        dayNamesMin: ['Do', 'Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'S&aacute;'],
        weekHeader: 'Sm',
        dateFormat: 'dd/mm/yy',
        firstDay: 1,
        isRTL: false,
        showMonthAfterYear: false,
        yearSuffix: ''
    };
    $.datepicker.setDefaults($.datepicker.regional['es']);


})(jQuery, this, document);

$(function () {
    $('input[type=text]').blur(function () {
        this.value = this.value.toUpperCase();
    });
    $("body").on('click', 'button', function () {

        // Si el boton no tiene el atributo ajax no hacemos nada
        if ($(this).data('ajax') === undefined) return;

        // El metodo .data identifica la entrada y la castea al valor más correcto
        if ($(this).data('ajax') !== true) return;

        var form = $(this).closest("form");
        var buttons = $("button", form);
        var button = $(this);
        var url = form.attr('action');

        if (button.data('confirm') !== undefined) {
            if (button.data('confirm') === '') {
                if (!confirm('¿Esta seguro de realizar esta acción?')) return false;
            } else {
                if (!confirm(button.data('confirm'))) return false;
            }
        }

        if (button.data('delete') !== undefined) {
            if (button.data('delete') === true) {
                url = button.data('url');
            }
        } else if (!form.valid()) {
            return false;
        }

        // Creamos un div que bloqueara todo el formulario
        var block = $('<div class="block-loading" />');
        form.prepend(block);

        // En caso de que haya habido un mensaje de alerta
        $("#card-alert", form).remove();

        // Para los formularios que tengan CKupdate
        //if (form.hasClass('CKupdate')) CKupdate();

        form.ajaxSubmit({
            dataType: 'JSON',
            type: 'POST',
            url: url,
            success: function (r) {
                block.remove();
                if (r.response) {
                    if (!button.data('reset') !== undefined) {
                        if (button.data('reset')) form.reset();
                    }
                    else {
                        form.find('input:file').val('');
                    }
                }

                // Mostrar mensaje
                if (r.message !== null) {
                    if (r.message.length > 0) {
                        var css = "";
                        if (r.response) css = "success";
                        else css = "error";

                        //var message = '<div id="card-alert" class="card ' + css + ' "><div class="card-content white-text"><p><i class="mdi-alert-error"></i>' + r.message + '</p></div><button type="button" class="close white-text" data-dismiss="alert" aria-label="Close"><span aria-hidden="true">×</span></button></div>';
                        var message = `<div id="card-mensaje-frm" class="alert ${css} ">
                                        <span class ="icon"></span>
					                    ${r.message}                                                                            
                                      </div>`;
                        form.prepend(message);
                        //setTimeout('$("#card-mensaje-frm .close").click(function () { $(this).closest("#card-mensaje-frm").fadeOut("slow") });', 0);
                        setTimeout('$("#card-mensaje-frm").fadeOut("slow");', 4000);
                    }
                }

                // Ejecutar funciones
                if (r.function != null) {
                    setTimeout(r.function, 0);
                }
                // Redireccionar
                if (r.href != null) {
                    if (r.href === 'self') window.location.reload(true);
                    else window.location.href = r.href;
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                block.remove();
                var message = `<div id="card-mensaje-error" class="card-alert card red">
                                        <div class="card-content white-text">
                                          <p>${textStatus}:${errorThrown}</p>
                                        </div>
                                        <button type="button" class="close white-text" data-dismiss="alert" aria-label="Close">
                                          <span aria-hidden="true">×</span>
                                        </button>
                                      </div>`;
                form.prepend(message);
                setTimeout('$("#card-mensaje-error .close").click(function () { $(this).closest("#card-mensaje-error").fadeOut("slow") });', 0);
                setTimeout('$("#card-mensaje-error").fadeOut("slow");', 4000);
            }
        });
        return false;
    });
});

var Vendix = {
    Url: function (strMethod) {
        return window.location.pathname + '/' + strMethod;
        //return 'http://localhost/CACMEA/' + strMethod;
    },

    Notificar: function (pOpcion) {
        if (pOpcion == 'Modificar') {
            $.jGrowl('SE MODIFICO CORRECTAMENTE!', { header: 'VENDIX Comercial', life: 2000 });
        } else if (pOpcion == 'Eliminar') {
            $.jGrowl('SE ELIMINO CORRECTAMENTE!', { header: 'VENDIX Comercial', life: 2000 });
        } else if (pOpcion == 'Anular') {
            $.jGrowl('SE ANULO CORRECTAMENTE!', { header: 'VENDIX Comercial', life: 2000 });
        } else if (pOpcion == 'Crear') {
            $.jGrowl('SE CREO EL REGISTRO CORRECTAMENTE!', { header: 'VENDIX Comercial', life: 2000 });
        } else {
            $.jGrowl('Se grabaron los datos correctamente!', { header: 'VENDIX Comercial', life: 2000 });
        }
    },
    Mensaje: function (pMensaje) {
        $.jGrowl(pMensaje, { header: 'VENDIX Comercial' });
    },
    Dialogo: function (pMensaje, pOpcion, callbackOk) {
        var dlg = $('<div id="dlgBase" <p > ' + pMensaje + '</p></div>')
            .dialog({
                title: "VENDIX",
                resizable: false,
                modal: true,
                width: 400,
                show: { effect: "slide", duration: 250 },
                hide: { effect: "slide", duration: 250 }
            });

        if (pOpcion == 'Aceptar') {
            dlg.dialog("option", "buttons", {
                "Aceptar": function () { $(this).dialog("close"); return true; }
            });
        }
        else if (pOpcion == 'SiNo') {
            dlg.dialog("option", "buttons", {
                "Si": function () { if (typeof callbackOk == 'function') { callbackOk.call(this); $(this).dialog("close"); } return true; },
                "No": function () { $(this).dialog("close"); return false; }
            });
        }
        else if (pOpcion == 'AceptarCancelar') {
            dlg.dialog("option", "buttons", {
                "Aceptar": function () { if (typeof callbackOk == 'function') { callbackOk.call(this); $(this).dialog("close"); } return true; },
                "Cancelar": function () { $(this).dialog("close"); return false; }
            });
        }
        else {
            dlg.dialog("option", "buttons", { "Aceptar": function () { $(this).dialog("close"); return true; } });
        }


        $('.ui-dialog-buttonset button:eq(0)').focus();
    },
    DialogoObs: function (pMensaje, callbackOk) {
        var dlg = $('<div id="dlgBase"><p>' + pMensaje + '</p> <textarea rows="4" id="txtBase" style="width: 100%" autofocus></textarea></div>')
            .dialog({
                title: "VENDIX",
                resizable: false,
                modal: true,
                width: 400,
                show: { effect: "slide", duration: 250 },
                hide: { effect: "slide", duration: 250 }
            });

        dlg.dialog("option", "buttons", {
            "Aceptar": function () {
                $.data(document.body, 'txtObsBase', $("#txtBase").val());
                if (typeof callbackOk == 'function') {
                    callbackOk.call(this);
                    $(this).dialog("close");
                }
                return true;
            },
            "Cancelar": function () { $(this).dialog("close"); return false; }
        });
        //$('.ui-dialog-buttonpane button:eq(0)').focus();
    },
    Reporte: function (strUrl) {

        $("<div><div id='loading-overlay'></div><div id='loading'><span>Cargando...</span></div>" +
            " <iframe src='" + strUrl + "' style='height:100%; width:100%;' onload='javascript:ocultarLoading();'></iframe></div>")
            .dialog({
                title: "VENDIX Reporte",
                resizable: false,
                modal: true,
                width: '60%',
                height: $(window).height() - 120,
                buttons: { "Cerrar": function () { $(this).dialog("close"); } }
            });
    },
    CargarCombo: function (strUrl, strComboId, callbackOk) {
        $.ajax({
            url: window.location.origin + strUrl,
            data: {},
            success: function (result) {
                if (result != null) {
                    var html = '';
                    //$("#" + strComboId).html("");

                    $.each(result, function () {
                        html += "<option value=\"" + this.Id + "\">" + this.Valor + "</option>";
                        //$("#" + strComboId).append($("<option></option>").attr("value", this.Id).text(this.Valor));
                    });
                    $("#" + strComboId).html(html);
                    $("#" + strComboId).trigger("liszt:updated");
                    if (typeof callbackOk == 'function') { callbackOk.call(this); }
                }
            }
        });
    },
    CargarCombo2: function (strUrl, strComboId, callbackOk) {
        $.ajax({
            url: window.location.origin + strUrl,
            data: {},
            success: function (result) {
                if (result != null) {
                    var html = '<select id="cbo' + strComboId + '">';
                    $.each(result, function () {
                        html += "<option value=\"" + this.Id + "\">" + this.Valor + "</option>";
                    });
                    html += "</select>";
                    $("#" + strComboId).html(html);
                    //$("#cbo" + strComboId).trigger("liszt:updated");
                    if (typeof callbackOk == 'function') { callbackOk.call(this); }
                }
            }
        });
    },
    TextoInline: function () {
        $('form').each(function () {
            $(this).find('label.inline').each(function () {
                var $label = $(this),
					$input = $('#' + $label.attr('for'));

                $input.css('padding-left', $label.outerWidth(true));
            });
        });
    }
};

var Constante = {
    FORMAPAGO: function (p) {
        switch (p) {
            case 'M': return 'Mensual';
            case 'Q': return 'Quincenal';
            case 'S': return 'Semanal';
            case 'D': return 'Diario';
            default: return ''
        }
    },
    CREDITO: function (p) {
        switch (p) {
            case 'PAG': return 'PAGADO';
            case 'PEN': return 'PENDIENTE';
            case 'AP1': return 'PRE APROBADO';
            case 'APR': return 'APROBADO';
            case 'DES': return 'DESEMBOLSADO';
            case 'REP': return 'REPROGRAMADO';
            case 'ANU': return 'ANULADO';
            case 'CRE': return 'CREADO';
            default: return ''
        }
    },
    TIPOGA: function (p) {
        switch (p) {
            case 'CAP': return 'GASTO ADM. DESCUENTO EN CAPITAL';
            case 'CUO': return 'GASTO ADM. EN CUOTAS';
            case 'ADE': return 'GASTO ADM. PAGO ADELANTADO';
            default: return ''
        }
    },
};
function ocultarLoading() {
    $("#loading,#loading-overlay").hide();
}
function IniListaPrecio() {

    $("#btnNuevo").click(function () {
        $("#txtArticulo").val("").removeAttr('disabled');
        $("#txtListaPrecio,#txtDescuento").val("0.00");
        $("#dlgNuevo").dialog("open");
        return false;
    });

    $("#btnBuscar").click(function () {
        $("#grdBuscar").jqGrid('setGridParam', { page: 1 }).trigger("reloadGrid");
    });
    $("#rbAsignado,#rbNoAsignado").change(function () {
        $("#grdBuscar").jqGrid('setGridParam', { page: 1 }).trigger("reloadGrid");
    });

    $("#txtBuscarxserie").keypress(function (event) {
        if (event.which == 13) {
            event.preventDefault();
            $.ajax({
                url: Vendix.Url('BuscarListaPrecio'),
                data: { pArticuloId: 0, pSerie: $("#txtBuscarxserie").val() },
                success: function (rpt) {
                    if (rpt != null) {
                        $("#txtArticulo").val(rpt.Denominacion);
                        $("#txtListaPrecio").val(rpt.Monto.toFixed(2));
                        $("#txtDescuento").val(rpt.Descuento.toFixed(2));
                        $("#chkActivo").prop("checked", rpt.Estado);
                        $.data(document.body, 'ListaPrecioId', rpt.ListaPrecioId);
                        $.data(document.body, 'ArticuloId', rpt.ArticuloId);
                        $('#txtListaPrecio').focus().select();
                    } else {
                        $("#txtBuscarxserie,#txtArticulo").val('');
                        $("#chkActivo").prop("checked", false);
                        $("#txtListaPrecio,#txtDescuento").val("0.00");
                        $.data(document.body, 'ListaPrecioId', 0);
                        $.data(document.body, 'ArticuloId', 0);
                    }
                }
            });
        }
    });

    $("#txtBuscar").keypress(function (event) {
        if (event.which == 13) {
            event.preventDefault();
            $("#grdBuscar").jqGrid('setGridParam', { page: 1 }).trigger("reloadGrid");
        }
    });
    $("#txtListaPxrecio,#txtDescuento,#txtDescuento,#txtPuntos").keypress(function (event) {
        if (event.which == 13) {
            event.preventDefault();
            GuardarDialogoListaPrecio();
        }
    });

    $("#dlgNuevo").dialog({
        autoOpen: false,
        modal: true,
        width: 400,
        open: function () {
            $(this).parent().css('overflow', 'visible');
            $$.utils.forms.resize();
        }
    }).find('button.submit').click(function () {
        GuardarDialogoListaPrecio();
    }).end().find('button.cancel').click(function () {
        var $el = $(this).parents('.ui-dialog-content');
        $el.find('form')[0].reset();
        $el.dialog('close');
    });

    $("#txtArticulo").autocomplete({
        source: Vendix.Url('BuscarArticulo'),
        minLength: 2,
        select: function (event, ui) {
            $.ajax({
                async: false,
                url: Vendix.Url('BuscarListaPrecio'),
                data: { pArticuloId: ui.item.Id, pSerie: "" },
                success: function (rpt) {
                    $("#txtBuscarxserie").val('');
                    if (rpt != null) {
                        $("#txtListaPrecio").val(rpt.Monto.toFixed(2));
                        $("#txtDescuento").val(rpt.Descuento.toFixed(2));
                        $("#chkActivo").prop("checked", rpt.Estado);
                        $.data(document.body, 'ListaPrecioId', rpt.ListaPrecioId);
                    } else {
                        $("#chkActivo").prop("checked", true);
                        $("#txtListaPrecio,#txtDescuento").val("0.00");
                        $.data(document.body, 'ListaPrecioId', 0);
                    }
                    $.data(document.body, 'ArticuloId', ui.item.Id);
                    $('#txtListaPrecio').focus().select();
                }
            });
        }
    });

    //$("#txtArticulo").autocomplete({
    //    source: function (request, response) {
    //        $.ajax({
    //            url: Vendix.Url('BuscarArticulo'),
    //            data: { pClave: request.term, maxRows: 8 },
    //            success: function (result) {
    //                response($.map(result, function (item) {
    //                    return {
    //                        label: item.Text,
    //                        value: item.Text,
    //                        Id: item.Value
    //                    };
    //                }));
    //            }
    //        });
    //    },
    //    minLength: 2,
    //    select: function (event, ui) {
    //        // alert(ui.item.Id);
    //        $.ajax({
    //            async: false,
    //            url: Vendix.Url('BuscarListaPrecio'),
    //            data: { pArticuloId: ui.item.Id, pSerie: "" },
    //            success: function (rpt) {
    //                $("#txtBuscarxserie").val('');
    //                if (rpt != null) {
    //                    $("#txtListaPrecio").val(rpt.Monto.toFixed(2));
    //                    $("#txtDescuento").val(rpt.Descuento.toFixed(2));
    //                    $("#chkActivo").prop("checked", rpt.Estado);
    //                    $.data(document.body, 'ListaPrecioId', rpt.ListaPrecioId);
    //                } else {
    //                    $("#chkActivo").prop("checked", true);
    //                    $("#txtListaPrecio,#txtDescuento").val("0.00");
    //                    $.data(document.body, 'ListaPrecioId', 0);
    //                }
    //                $.data(document.body, 'ArticuloId', ui.item.Id);
    //                $('#txtListaPrecio').focus().select();
    //            }
    //        });


    //    }
    //});

    $('#grdBuscar').jqGrid({
        url: Vendix.Url("Listar"),
        datatype: 'json',
        postData: {
            filters: function () {
                return $.toJSON([{ Key: "Buscar", Value: $("#txtBuscar").val() }, { Key: "TipoArticuloId", Value: $("#cboTipoArticulo").val() }, { Key: "Asignado", Value: $("#rbAsignado").prop('checked') }]);
            }
        },
        colNames: ['ArticuloId', 'ListaPrecioId', 'Tipo Articulo', 'Articulo', 'Precio Venta', 'Descuento Max', 'Puntos Canje.', 'Puntos Asign.', 'Estado', 'Estado'],
        colModel: [
            { name: 'ArticuloId', index: 'ArticuloId', hidden: true },
            { name: 'ListaPrecioId', index: 'ListaPrecioId', hidden: true },
            { name: 'TipoArticulo', index: 'TipoArticulo', align: 'left', width: 100 },
            { name: 'Articulo', index: 'Denominacion', align: 'left', width: 250 },
            { name: 'Monto', index: 'Monto', align: 'right', width: 80, formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2, prefix: "S/. " } },
            { name: 'Descuento', index: 'Descuento', align: 'right', width: 80, formatter: 'currency', formatoptions: { decimalSeparator: ".", thousandsSeparator: ",", decimalPlaces: 2, prefix: "S/. " } },
            { name: 'PuntosCanje', index: 'PuntosCanje', align: 'right', width: 80 },
            { name: 'Puntos', index: 'Puntos', align: 'right', width: 80 },
            { name: 'Estado', index: 'Estado', hidden: true },
            { name: 'Activo', index: 'Activo', align: 'center', width: 80, formatter: ActivoFormatter }
        ],
        caption: "Lista de Precios",
        pager: $('#grdpBuscar'),
        rowNum: 15,
        rowList: [15, 30, 45],
        sortname: 'TipoArticulo',
        sortorder: 'asc',
        gridview: true,
        viewrecords: true,
        rownumbers: true,
        autowidth: true,
        //shrinkToFit: false,
        width: 'auto',
        height: '347px',
        ondblClickRow: function () {
            var id = jQuery("#grdBuscar").jqGrid('getGridParam', 'selrow');
            if (id) {
                var ret = $("#grdBuscar").jqGrid('getRowData', id);
                $("#txtArticulo").val(ret.Articulo).attr('disabled', 'disabled');
                $("#chkActivo").prop("checked", ret.Estado == "True" ? true : false);
                $("#txtListaPrecio").val(ret.Monto);
                $("#txtPuntosCanje").val(ret.PuntosCanje);
                $("#txtPuntos").val(ret.Puntos);
                $("#txtDescuento").val(ret.Descuento);
                $.data(document.body, 'ListaPrecioId', ret.ListaPrecioId);
                $.data(document.body, 'ArticuloId', ret.ArticuloId);

                $("#dlgNuevo").dialog("open");
                $("#txtListaPrecio").focus().select();
            }
        }
    });

}

function GuardarDialogoListaPrecio() {
    var $el = $("#dlgNuevo");
    if ($el.validate().form()) {
        if ($.data(document.body, 'ArticuloId') == null || $.data(document.body, 'ArticuloId') == 0) {
            Vendix.Dialogo("ERROR: Seleccione un Articulo.", "Aceptar");
        }
        $.ajax({
            type: 'POST',
            url: Vendix.Url('GuardarListaPrecio'),
            data: {
                pListaPrecioId: $.data(document.body, 'ListaPrecioId'),
                pArticuloId: $.data(document.body, 'ArticuloId'),
                pPrecio: $("#txtListaPrecio").val(),
                pDescuento: $("#txtDescuento").val(),
                pPuntos: $("#txtPuntos").val(),
                pPuntosCanje: $("#txtPuntosCanje").val(),
                pActivo: $("#chkActivo").prop('checked')
            },
            success: function (data) {
                Vendix.Notificar("Modificar");
                $("#grdBuscar").trigger("reloadGrid");
                $el.find('form')[0].reset();
                $el.dialog('close');
            }
        });
    }
}

function ActivoFormatter(cellvalue, options, rowObject) {
    var arr = cellvalue.split(',');
    if (arr[0] == '0') return "";
    if (arr[1] == '1')
        return "<a href='' onclick='Activar(" + arr[0] + ");return false;'><img src='img/icons/packs/fugue/16x16/tick_in.png' title='Activo' /></a>";
    else
        return "<a href='' onclick='Activar(" + arr[0] + ");return false;'><img src='img/icons/packs/iconsweets2/16x16/download.png' title='Anulado' /></a>";
};

function DeleteFormatter(cellvalue, options, rowObject) {
    return "<a href='#' onclick='Delete(" + cellvalue + ");return false;' class='button small grey tooltip' data-gravity='s' title='Eliminar'><i class='icon-remove'></i></a>";
}
function Delete(id) {
    $.ajax({
        url: Vendix.Url("Delete"),
        type: 'POST',
        data: { pid: id },
        success: function (rpt) {
            Vendix.Notificar('Eliminar');
            $(".grdDelete").trigger("reloadGrid");
        }
    });
}
function Activar(id) {
    $.ajax({
        url: Vendix.Url("Activar"),
        type: 'POST',
        data: { pid: id },
        success: function (rpt) {
            Vendix.Notificar();
            $("#grdBuscar").trigger("reloadGrid");
        }
    });
}


