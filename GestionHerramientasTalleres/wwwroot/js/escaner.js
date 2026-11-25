
window.escanerHerramientas = (() => {

    let lector = null;
    let controles = null;
    let sesion = 0;

    // ========================================
    // VERIFICAR BIBLIOTECA ZXING
    // ========================================

    function verificarBiblioteca() {

        if (typeof ZXingBrowser === "undefined") {
            throw new Error(
                "No se pudo cargar la biblioteca ZXing."
            );
        }
    }

    // ========================================
    // DETENER CAMARA
    // ========================================

    function detenerCamara() {

        sesion++;

        if (controles) {
            controles.stop();
            controles = null;
        }

        lector = null;
    }

    // ========================================
    // INICIAR ESCANEO CON CAMARA
    // ========================================

    async function iniciarCamara(videoId, dotNetHelper) {

        detenerCamara();

        const sesionActual = sesion;

        try {

            verificarBiblioteca();

            if (!navigator.mediaDevices?.getUserMedia) {
                throw new Error(
                    "La camara requiere una conexion HTTPS segura."
                );
            }

            const video = document.getElementById(videoId);

            if (!video) {
                throw new Error(
                    "No se encontro el elemento de video."
                );
            }

            lector = new ZXingBrowser.BrowserMultiFormatReader();

            let detectado = false;

            const nuevosControles =
                await lector.decodeFromConstraints(
                    {
                        audio: false,
                        video: {
                            facingMode: {
                                ideal: "environment"
                            }
                        }
                    },
                    video,
                    async (resultado, error, control) => {

                        if (sesionActual !== sesion || detectado) {
                            return;
                        }

                        if (resultado) {

                            detectado = true;

                            const codigo = resultado
                                .getText()
                                .trim();

                            control.stop();
                            detenerCamara();

                            await dotNetHelper.invokeMethodAsync(
                                "CodigoDetectado",
                                codigo
                            );
                        }
                    }
                );

            if (sesionActual !== sesion || detectado) {
                nuevosControles.stop();
            } else {
                controles = nuevosControles;
            }

        } catch (error) {

            if (sesionActual !== sesion) {
                return;
            }

            detenerCamara();

            await dotNetHelper.invokeMethodAsync(
                "ErrorEscaner",
                error.message || "No se pudo iniciar la camara."
            );
        }
    }

    // ========================================
    // ESCANEAR DESDE UNA FOTOGRAFIA
    // ========================================

    async function escanearImagen(inputId, dotNetHelper) {

        let urlImagen = null;

        try {

            detenerCamara();
            verificarBiblioteca();

            const input = document.getElementById(inputId);

            if (!input || !input.files || !input.files.length) {
                return;
            }

            const archivo = input.files[0];

            if (!archivo.type.startsWith("image/")) {
                throw new Error(
                    "Selecciona una imagen valida."
                );
            }

            urlImagen = URL.createObjectURL(archivo);

            const lectorImagen =
                new ZXingBrowser.BrowserMultiFormatReader();

            const resultado =
                await lectorImagen.decodeFromImageUrl(urlImagen);

            const codigo = resultado
                .getText()
                .trim();

            await dotNetHelper.invokeMethodAsync(
                "CodigoDetectado",
                codigo
            );

        } catch (error) {

            await dotNetHelper.invokeMethodAsync(
                "ErrorEscaner",
                "No se pudo reconocer el codigo. Intenta con una imagen mas clara."
            );

        } finally {

            if (urlImagen) {
                URL.revokeObjectURL(urlImagen);
            }

            const input = document.getElementById(inputId);

            if (input) {
                input.value = "";
            }
        }
    }

    // ========================================
    // FUNCIONES DISPONIBLES PARA BLAZOR
    // ========================================

    return {
        iniciarCamara,
        detenerCamara,
        escanearImagen
    };

})();
