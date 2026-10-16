package com.app.nutriredapp.data.network

import android.util.Log
import com.app.nutriredapp.data.model.Category
import com.app.nutriredapp.data.model.Donation
import com.app.nutriredapp.data.model.Donor
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.data.model.Product
import com.app.nutriredapp.data.model.User
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.TimeUnit

object NutriRedApiClient {

    private const val TAG = "NutriRedApiClient"
    private const val PREFS_NAME = "NutriRedNetworkPrefs"
    private const val KEY_CUSTOM_URL = "custom_server_url"

    private var sharedPreferences: android.content.SharedPreferences? = null

    // URL descubierta y confirmada que funciona activamente
    @Volatile
    var activeBaseUrl: String? = null

    // URL configurada manualmente por el usuario en la app (ej: "http://192.168.100.42:5137/api/")
    @Volatile
    var customBaseUrl: String? = null

    // 192.168.100.42 es la IP de la máquina de desarrollo en la red Wi-Fi
    // 10.0.2.2 es el alias del emulador oficial Android Studio hacia el localhost anfitrión
    var baseUrl: String = "http://192.168.100.42:5137/api/"
    var fallbackUrl: String = "http://10.0.2.2:5137/api/"
    var isNetworkEnabled: Boolean = true
    var ioDispatcher: kotlinx.coroutines.CoroutineDispatcher = Dispatchers.IO

    private val defaultCandidates = listOf(
        "http://192.168.100.42:5137/api/",
        "http://10.0.2.2:5137/api/",
        "http://10.0.3.2:5137/api/",
        "http://localhost:5137/api/",
        "http://127.0.0.1:5137/api/"
    )

    fun init(context: android.content.Context) {
        if (sharedPreferences == null) {
            sharedPreferences = context.applicationContext.getSharedPreferences(PREFS_NAME, android.content.Context.MODE_PRIVATE)
            val savedUrl = sharedPreferences?.getString(KEY_CUSTOM_URL, null)
            if (!savedUrl.isNullOrBlank()) {
                val formatted = formatUrl(savedUrl)
                customBaseUrl = formatted
                activeBaseUrl = formatted
                Log.i(TAG, "Cargada URL de servidor guardada desde SharedPreferences: $formatted")
            }
        }
    }

    fun setCustomServerUrl(urlOrHost: String?) {
        val clean = urlOrHost?.trim() ?: ""
        val formatted = if (clean.isBlank()) null else formatUrl(clean)
        customBaseUrl = formatted
        activeBaseUrl = formatted
        sharedPreferences?.edit()?.let { editor ->
            if (formatted != null) {
                editor.putString(KEY_CUSTOM_URL, formatted)
            } else {
                editor.remove(KEY_CUSTOM_URL)
            }
            editor.apply()
        }
        Log.i(TAG, "URL de servidor configurada: $formatted")
    }

    fun formatUrl(input: String): String {
        var clean = input.trim()
        if (!clean.startsWith("http://") && !clean.startsWith("https://")) {
            clean = if (clean.contains(":")) "http://$clean" else "http://$clean:5137"
        }
        clean = clean.trimEnd('/')
        return if (clean.endsWith("/api")) "$clean/" else if (clean.endsWith("/api/")) clean else "$clean/api/"
    }

    fun getCandidateUrls(): List<String> {
        val list = mutableListOf<String>()
        customBaseUrl?.let { list.add(formatUrl(it)) }
        activeBaseUrl?.let { list.add(formatUrl(it)) }
        list.add(formatUrl(baseUrl))
        list.add(formatUrl(fallbackUrl))
        list.addAll(defaultCandidates.map { formatUrl(it) })
        return list.distinct()
    }

    data class ConnectionCheckResult(
        val success: Boolean,
        val url: String,
        val message: String
    )

    suspend fun testConnection(targetUrl: String? = null): ConnectionCheckResult = withContext(ioDispatcher) {
        val urlsToTest = if (!targetUrl.isNullOrBlank()) {
            listOf(formatUrl(targetUrl))
        } else {
            getCandidateUrls()
        }

        for (candidate in urlsToTest) {
            try {
                val req = Request.Builder().url("${candidate.trimEnd('/')}/categorias").get().build()
                val resp = probeClient.newCall(req).execute()
                resp.use { r ->
                    if (r.isSuccessful || r.code == 401 || r.code == 403 || r.code == 404) {
                        activeBaseUrl = candidate
                        return@withContext ConnectionCheckResult(
                            success = true,
                            url = candidate,
                            message = "Conexión exitosa al servidor .NET ($candidate)"
                        )
                    }
                }
            } catch (e: Exception) {
                Log.d(TAG, "Prueba a $candidate no respondió: ${e.message}")
            }
        }

        return@withContext ConnectionCheckResult(
            success = false,
            url = targetUrl ?: (activeBaseUrl ?: baseUrl),
            message = "No se pudo conectar a ninguna dirección del backend. Verifique que la PC tenga iniciado el servidor y esté en la misma red Wi-Fi."
        )
    }

    private val json = Json {
        ignoreUnknownKeys = true
        coerceInputValues = true
        isLenient = true
        encodeDefaults = true
    }

    private val client = OkHttpClient.Builder()
        .connectTimeout(3, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .build()

    private val probeClient = OkHttpClient.Builder()
        .connectTimeout(2, TimeUnit.SECONDS)
        .readTimeout(3, TimeUnit.SECONDS)
        .build()

    private val JSON_MEDIA_TYPE = "application/json; charset=utf-8".toMediaType()

    private fun executeWithFallback(requestBuilder: (String) -> Request): okhttp3.Response? {
        if (!isNetworkEnabled) return null

        // 1. Si ya tenemos una URL activa conocida, intentar con ella primero directamente
        val active = activeBaseUrl
        if (!active.isNullOrBlank()) {
            try {
                val req = requestBuilder(active.trimEnd('/'))
                val resp = client.newCall(req).execute()
                return resp
            } catch (e: Exception) {
                Log.w(TAG, "Fallo de conexión a activeBaseUrl ($active): ${e.message}. Probando candidatos...")
                activeBaseUrl = null
            }
        }

        // 2. Probar candidatos disponibles
        val urlsToTry = getCandidateUrls()
        for (candidate in urlsToTry) {
            try {
                val req = requestBuilder(candidate.trimEnd('/'))
                val resp = client.newCall(req).execute()
                activeBaseUrl = candidate
                Log.i(TAG, "Conexión exitosa y fijada a activeBaseUrl: $candidate")
                return resp
            } catch (e: Exception) {
                Log.w(TAG, "Intento de conexión a $candidate falló: ${e.message}")
            }
        }
        return null
    }

    // ==========================================
    // PRODUCTOS (Catálogo y Escaneo de Código de Barras)
    // ==========================================

    suspend fun getProductByBarcode(barcode: String): Product? = withContext(ioDispatcher) {
        val cleanBarcode = barcode.trim()
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/productos/barcode/$cleanBarcode").get().build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "getProductByBarcode HTTP ${resp.code} para $cleanBarcode")
                    return@withContext null
                }
                val bodyStr = resp.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Product>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getProductByBarcode: ${e.message}")
            return@withContext null
        }
    }

    suspend fun getAllProducts(): List<Product> = withContext(ioDispatcher) {
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/productos").get().build()
            } ?: return@withContext emptyList()

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "getAllProducts HTTP ${resp.code}")
                    return@withContext emptyList()
                }
                val bodyStr = resp.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Product>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getAllProducts: ${e.message}")
            return@withContext emptyList()
        }
    }

    suspend fun searchProducts(query: String): List<Product> = withContext(ioDispatcher) {
        val encodedQuery = java.net.URLEncoder.encode(query.trim(), "UTF-8")
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/productos/search?q=$encodedQuery").get().build()
            } ?: return@withContext emptyList()

            response.use { resp ->
                if (!resp.isSuccessful) return@withContext emptyList()
                val bodyStr = resp.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Product>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en searchProducts: ${e.message}")
            return@withContext emptyList()
        }
    }

    // ==========================================
    // CATEGORÍAS (Clasificación de Alimentos)
    // ==========================================

    suspend fun getAllCategories(): List<Category> = withContext(ioDispatcher) {
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/categorias").get().build()
            } ?: return@withContext emptyList()

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "getAllCategories HTTP ${resp.code}")
                    return@withContext emptyList()
                }
                val bodyStr = resp.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Category>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getAllCategories: ${e.message}")
            return@withContext emptyList()
        }
    }

    @Serializable
    private data class CrearProductoRequest(
        val barcode: String,
        val name: String,
        val unitOfMeasure: String = "unidades",
        val categoriaId: Int? = null,
        val categoryName: String? = null,
        val category: String? = null
    )

    suspend fun createProduct(product: Product): Product? = withContext(ioDispatcher) {
        try {
            val reqPayload = CrearProductoRequest(
                barcode = product.barcode,
                name = product.name,
                unitOfMeasure = product.unitOfMeasure,
                categoriaId = product.categoryId,
                categoryName = product.category,
                category = product.category
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/productos").post(jsonBody).build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "createProduct HTTP ${resp.code}")
                    return@withContext null
                }
                val bodyStr = resp.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Product>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en createProduct: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // DONANTES (Búsqueda y Registro)
    // ==========================================

    suspend fun findDonorByDocument(documentNumber: String): Donor? = withContext(ioDispatcher) {
        val cleanDoc = documentNumber.trim()
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/donantes/buscar?documento=$cleanDoc").get().build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) return@withContext null
                val bodyStr = resp.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Donor>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en findDonorByDocument: ${e.message}")
            return@withContext null
        }
    }

    suspend fun getAllDonors(): List<Donor> = withContext(ioDispatcher) {
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/donantes").get().build()
            } ?: return@withContext emptyList()

            response.use { resp ->
                if (!resp.isSuccessful) return@withContext emptyList()
                val bodyStr = resp.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Donor>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en getAllDonors: ${e.message}")
            return@withContext emptyList()
        }
    }

    @Serializable
    private data class GuardarDonanteRequest(
        val type: String,
        val documentNumber: String?,
        val name: String,
        val phone: String?,
        val email: String?
    )

    suspend fun saveDonor(donor: Donor): Donor? = withContext(ioDispatcher) {
        try {
            val reqPayload = GuardarDonanteRequest(
                type = donor.type.name,
                documentNumber = donor.documentNumber,
                name = donor.name ?: "Donante sin nombre",
                phone = donor.phone,
                email = donor.email
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/donantes").post(jsonBody).build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) return@withContext null
                val bodyStr = resp.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Donor>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en saveDonor: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // DONACIONES (Registro Oficial y Descarga de Comprobante)
    // ==========================================

    @Serializable
    private data class RegistrarDonacionItemRequest(
        val productBarcode: String,
        val productName: String,
        val quantity: Double,
        val unitOfMeasure: String,
        val expirationDate: String,
        val batchNumber: String? = null,
        val categoryId: Int? = null,
        val categoryName: String? = null
    )

    @Serializable
    private data class RegistrarDonacionApiRequest(
        val donorType: String?,
        val documentNumber: String?,
        val donorName: String?,
        val phone: String?,
        val email: String?,
        val volunteerId: String,
        val volunteerName: String,
        val items: List<RegistrarDonacionItemRequest>
    )

    @Serializable
    private data class RegistrarDonacionResponse(
        val success: Boolean,
        val id: Int,
        val donationCode: String? = null,
        val timestamp: String? = null,
        val message: String? = null
    )

    suspend fun submitDonation(donation: Donation): Donation? = withContext(ioDispatcher) {
        try {
            val itemsPayload = donation.items.map { item ->
                RegistrarDonacionItemRequest(
                    productBarcode = item.productBarcode,
                    productName = item.productName,
                    quantity = item.quantity,
                    unitOfMeasure = item.unitOfMeasure,
                    expirationDate = item.expirationDate,
                    batchNumber = item.batchNumber,
                    categoryId = item.categoryId,
                    categoryName = item.categoryName
                )
            }

            val reqPayload = RegistrarDonacionApiRequest(
                donorType = donation.donor?.type?.name,
                documentNumber = donation.donor?.documentNumber,
                donorName = donation.donor?.name,
                phone = donation.donor?.phone,
                email = donation.donor?.email,
                volunteerId = donation.volunteerId,
                volunteerName = donation.volunteerName,
                items = itemsPayload
            )

            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/donaciones").post(jsonBody).build()
            } ?: executeWithFallback { base ->
                Request.Builder().url("$base/donaciones/registrar").post(jsonBody).build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) {
                    val err = resp.body?.string() ?: ""
                    Log.w(TAG, "submitDonation falló HTTP ${resp.code}: $err")
                    val parsedMsg = try {
                        val obj = json.decodeFromString<RegistrarDonacionResponse>(err)
                        obj.message
                    } catch (_: Exception) { null }
                    val finalMsg = parsedMsg ?: if (err.isNotBlank() && err.length < 250) err else "Error HTTP ${resp.code} al registrar donación."
                    throw IllegalStateException(finalMsg)
                }
                val bodyStr = resp.body?.string() ?: return@withContext null
                val res = json.decodeFromString<RegistrarDonacionResponse>(bodyStr)
                return@withContext donation.copy(
                    id = res.id,
                    donationCode = res.donationCode ?: donation.donationCode,
                    timestamp = res.timestamp ?: donation.timestamp
                )
            }
        } catch (e: IllegalStateException) {
            // Propagar el error de validación de negocio al ViewModel para mostrarlo en pantalla
            throw e
        } catch (e: Exception) {
            Log.e(TAG, "Error de red en submitDonation: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // ENTREGAS (Despacho con Firma Digital)
    // ==========================================

    suspend fun getPreparedPackagesResult(): Result<List<FoodPackage>> = withContext(ioDispatcher) {
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/entregas/paquetes-preparados").get().build()
            } ?: return@withContext Result.failure(Exception("No se pudo conectar con el servidor"))

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "getPreparedPackages HTTP ${resp.code}")
                    return@withContext Result.failure(Exception("HTTP ${resp.code}"))
                }
                val bodyStr = resp.body?.string() ?: "[]"
                val packages = json.decodeFromString<List<FoodPackage>>(bodyStr)
                return@withContext Result.success(packages)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en getPreparedPackages: ${e.message}")
            return@withContext Result.failure(e)
        }
    }

    suspend fun getPreparedPackages(): List<FoodPackage> =
        getPreparedPackagesResult().getOrDefault(emptyList())

    suspend fun getPackageByQr(packageCode: String): FoodPackage? = withContext(ioDispatcher) {
        val cleanCode = packageCode.trim()
        try {
            val response = executeWithFallback { base ->
                Request.Builder().url("$base/entregas/paquete/$cleanCode").get().build()
            } ?: return@withContext null

            response.use { resp ->
                if (!resp.isSuccessful) {
                    Log.w(TAG, "getPackageByQr HTTP ${resp.code} para $cleanCode")
                    return@withContext null
                }
                val bodyStr = resp.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<FoodPackage>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error buscando paquete por QR: ${e.message}")
            return@withContext null
        }
    }

    @Serializable
    private data class ConfirmarEntregaRequest(
        val packageId: String?,
        val packageCode: String?,
        val receiverDni: String,
        val receiverName: String,
        val isTitular: Boolean = true,
        val signatureBase64: String? = null,
        val volunteerId: String = "VOL-01"
    )

    suspend fun confirmDelivery(
        packageId: String,
        packageCode: String,
        receiverDni: String,
        receiverName: String,
        signatureBase64: String,
        volunteerId: String
    ): Boolean = withContext(ioDispatcher) {
        try {
            val reqPayload = ConfirmarEntregaRequest(
                packageId = packageId,
                packageCode = packageCode,
                receiverDni = receiverDni,
                receiverName = receiverName,
                isTitular = true,
                signatureBase64 = signatureBase64,
                volunteerId = volunteerId
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/entregas/confirmar").post(jsonBody).build()
            } ?: return@withContext false

            response.use { resp ->
                return@withContext resp.isSuccessful
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en confirmDelivery: ${e.message}")
            return@withContext false
        }
    }

    @Serializable
    private data class EntregaFallidaApiRequest(
        val packageCode: String,
        val motivoNoEntrega: String,
        val volunteerId: String = "VOL-01",
        val volunteerName: String = "Voluntario Móvil"
    )

    suspend fun registerFailedDelivery(
        packageCode: String,
        reason: String,
        volunteerName: String = "Voluntario Móvil"
    ): Boolean = withContext(ioDispatcher) {
        try {
            val reqPayload = EntregaFallidaApiRequest(
                packageCode = packageCode.trim(),
                motivoNoEntrega = reason.trim(),
                volunteerName = volunteerName.trim()
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/entregas/fallida").post(jsonBody).build()
            } ?: return@withContext false

            response.use { resp ->
                if (!resp.isSuccessful) {
                    val err = resp.body?.string()
                    Log.w(TAG, "registerFailedDelivery HTTP ${resp.code}: $err")
                }
                return@withContext resp.isSuccessful
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en registerFailedDelivery: ${e.message}")
            return@withContext false
        }
    }

    // ==========================================
    // AUTENTICACIÓN (Login Voluntario / Admin)
    // ==========================================

    @Serializable
    private data class LoginRequest(
        val email: String,
        val password: String
    )

    @Serializable
    data class LoginResponse(
        val success: Boolean,
        val token: String? = null,
        val message: String? = null,
        val user: User? = null
    )

    suspend fun login(email: String, password: String): Result<User> = withContext(ioDispatcher) {
        try {
            val reqPayload = LoginRequest(email = email.trim(), password = password.trim())
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)

            val response = executeWithFallback { base ->
                Request.Builder().url("$base/auth/login").post(jsonBody).build()
            } ?: return@withContext Result.failure(Exception("No se pudo conectar al servidor local en $baseUrl ni $fallbackUrl"))

            response.use { resp ->
                val bodyStr = resp.body?.string() ?: ""
                if (resp.isSuccessful) {
                    val loginRes = json.decodeFromString<LoginResponse>(bodyStr)
                    val user = loginRes.user ?: User(email = email, fullName = "Voluntario")
                    return@withContext Result.success(user)
                } else {
                    val msg = try {
                        val errObj = json.decodeFromString<LoginResponse>(bodyStr)
                        errObj.message ?: "Credenciales inválidas."
                    } catch (_: Exception) {
                        "Error HTTP ${resp.code} al iniciar sesión."
                    }
                    return@withContext Result.failure(Exception(msg))
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de red en login: ${e.message}")
            return@withContext Result.failure(e)
        }
    }
}
