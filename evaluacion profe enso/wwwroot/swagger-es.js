// Traduce al español la interfaz de Swagger UI
(function () {
  var t = {
    'Try it out': 'Probar', 'Cancel': 'Cancelar', 'Execute': 'Ejecutar', 'Clear': 'Limpiar',
    'Authorize': 'Autorizar', 'Logout': 'Cerrar sesión', 'Close': 'Cerrar',
    'Available authorizations': 'Autorizaciones disponibles',
    'Parameters': 'Parámetros', 'No parameters': 'Sin parámetros', 'Description': 'Descripción', 'Name': 'Nombre',
    'Responses': 'Respuestas', 'Response body': 'Cuerpo de la respuesta', 'Response headers': 'Encabezados de la respuesta',
    'Request body': 'Cuerpo de la solicitud', 'Request URL': 'URL de la solicitud', 'Server response': 'Respuesta del servidor',
    'Code': 'Código', 'Details': 'Detalles', 'Links': 'Enlaces', 'No links': 'Sin enlaces', 'Media type': 'Tipo de contenido',
    'Example Value': 'Ejemplo', 'Schema': 'Esquema', 'Schemas': 'Esquemas',
    'Controls Accept header.': 'Controla el encabezado Accept.', 'Controls': 'Controla el encabezado', 'header.': '.',
    'Select a definition': 'Seleccione una definición', 'Skip to operations': 'Ir a las operaciones',
    'Download': 'Descargar', 'Curl': 'Curl', 'required': 'obligatorio', 'Required': 'Obligatorio',
    'Success': 'Correcto', 'OK': 'Correcto', 'Created': 'Creado', 'No Content': 'Sin contenido',
    'Bad Request': 'Solicitud incorrecta', 'Unauthorized': 'No autorizado', 'Forbidden': 'Acceso denegado',
    'Not Found': 'No encontrado', 'Conflict': 'Conflicto', 'Internal Server Error': 'Error interno del servidor',
    'Client Error': 'Error del cliente', 'Server Error': 'Error del servidor',
    'Loading...': 'Cargando...', 'No operations defined in spec!': 'No hay operaciones definidas.',
    'Value': 'Valor', 'Authorized': 'Autorizado', 'Filter by tag': 'Filtrar por etiqueta',
    'Expand operations': 'Expandir operaciones', 'Collapse operations': 'Contraer operaciones',
    'Show/Hide': 'Mostrar/Ocultar', 'List Operations': 'Listar operaciones', 'Expand Operations': 'Expandir operaciones',
    'Try it out ': 'Probar', 'Reset': 'Restablecer', 'Copy': 'Copiar', 'Send empty value': 'Enviar valor vacío',
    'Add item': 'Agregar elemento', 'Add string item': 'Agregar texto', 'Add integer item': 'Agregar número',
    'Parameter content type': 'Tipo de contenido del parámetro', 'Model': 'Modelo',
    'Request duration': 'Duración de la solicitud'
  };

  function traducir(raiz) {
    var w = document.createTreeWalker(raiz, NodeFilter.SHOW_TEXT);
    var n;
    while ((n = w.nextNode())) {
      var v = n.nodeValue;
      var limpio = v.trim();
      if (limpio && t[limpio] && !n.parentElement.closest('pre, code, .microlight, textarea')) {
        n.nodeValue = v.replace(limpio, t[limpio]);
      }
    }
    raiz.querySelectorAll && raiz.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(function (e) {
      var p = e.getAttribute('placeholder');
      if (p === 'Filter by tag') e.setAttribute('placeholder', 'Filtrar por etiqueta');
    });
  }

  var pendiente = false;
  new MutationObserver(function () {
    if (pendiente) return;
    pendiente = true;
    setTimeout(function () { pendiente = false; traducir(document.body); }, 50);
  }).observe(document.documentElement, { childList: true, subtree: true, characterData: true });

  document.addEventListener('DOMContentLoaded', function () { traducir(document.body); });
})();
