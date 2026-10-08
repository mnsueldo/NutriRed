package com.app.nutriredapp.data.repository

import android.util.Log
import com.app.nutriredapp.data.model.Donor
import com.app.nutriredapp.data.model.DonorType
import com.app.nutriredapp.data.network.NutriRedApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class DonorRepository {

    private val TAG = "DonorRepository"

    // Cache en memoria para soporte offline y funcionamiento fluido en depósitos
    private val memoryDonors = mutableListOf(
        Donor(
            id = "DONOR-001",
            type = DonorType.INDIVIDUAL,
            documentNumber = "30123456",
            name = "Juan Pérez",
            phone = "11-4567-8901",
            email = "juan.perez@email.com"
        ),
        Donor(
            id = "DONOR-002",
            type = DonorType.INSTITUTION,
            documentNumber = "30-71234567-9",
            name = "Supermercados El Ahorro S.A.",
            phone = "11-4000-5555",
            email = "contacto@elahorro.com.ar"
        ),
        Donor(
            id = "DONOR-003",
            type = DonorType.INDIVIDUAL,
            documentNumber = "28999888",
            name = "María Gómez",
            phone = "11-6677-8899",
            email = "maria.gomez@email.com"
        )
    )

    suspend fun findDonorByDocument(documentNumber: String): Donor? = withContext(Dispatchers.IO) {
        val cleanDoc = documentNumber.trim()

        // 1. Intentar consultar en la API central ASP.NET Core
        try {
            val remote = NutriRedApiClient.findDonorByDocument(cleanDoc)
            if (remote != null) {
                val idx = memoryDonors.indexOfFirst { it.documentNumber == cleanDoc }
                if (idx != -1) {
                    memoryDonors[idx] = remote
                } else {
                    memoryDonors.add(remote)
                }
                Log.d(TAG, "Donante encontrado en API central: ${remote.name}")
                return@withContext remote
            }
        } catch (e: Exception) {
            Log.w(TAG, "Fallo al consultar donante en API: ${e.message}")
        }

        // 2. Fallback a caché en memoria
        val cached = memoryDonors.find {
            it.documentNumber?.replace(".", "")?.replace("-", "") ==
                    cleanDoc.replace(".", "").replace("-", "")
        }
        if (cached != null) {
            Log.d(TAG, "Donante recuperado de memoria: ${cached.name}")
            return@withContext cached
        }

        return@withContext null
    }

    suspend fun saveDonor(donor: Donor): Donor = withContext(Dispatchers.IO) {
        val donorWithId = if (donor.id == null) {
            donor.copy(id = "DONOR-${System.currentTimeMillis() % 100000}")
        } else {
            donor
        }

        // 1. Intentar persistir en API central
        try {
            val savedRemote = NutriRedApiClient.saveDonor(donorWithId)
            if (savedRemote != null) {
                memoryDonors.add(savedRemote)
                Log.d(TAG, "Donante registrado en API central: ${savedRemote.name}")
                return@withContext savedRemote
            }
        } catch (e: Exception) {
            Log.w(TAG, "Fallo al guardar donante en API: ${e.message}")
        }

        // 2. Contingencia local
        memoryDonors.add(donorWithId)
        return@withContext donorWithId
    }

    suspend fun getAllDonors(): List<Donor> = withContext(Dispatchers.IO) {
        try {
            val remote = NutriRedApiClient.getAllDonors()
            if (remote.isNotEmpty()) {
                remote.forEach { r ->
                    val idx = memoryDonors.indexOfFirst { it.documentNumber == r.documentNumber }
                    if (idx != -1) memoryDonors[idx] = r else memoryDonors.add(r)
                }
                return@withContext memoryDonors.toList()
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error listando donantes de API: ${e.message}")
        }
        memoryDonors.toList()
    }
}
