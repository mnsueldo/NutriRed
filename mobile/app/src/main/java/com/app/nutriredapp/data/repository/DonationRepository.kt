package com.app.nutriredapp.data.repository

import android.util.Log
import com.app.nutriredapp.data.model.Donation
import com.app.nutriredapp.data.network.NutriRedApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.util.Calendar

class DonationRepository {

    private val TAG = "DonationRepository"
    private val memoryDonations = mutableListOf<Donation>()
    private var sequenceCounter = 44

    suspend fun saveDonation(donation: Donation): Donation = withContext(Dispatchers.IO) {
        // 1. Intentar registrar en la API central ASP.NET Core (afecta inventario real en SQL Server)
        try {
            val remoteResult = NutriRedApiClient.submitDonation(donation)
            if (remoteResult != null) {
                memoryDonations.add(remoteResult)
                Log.d(TAG, "Donación registrada con éxito en backend .NET con comprobante: ${remoteResult.donationCode}")
                return@withContext remoteResult
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error enviando donación a API: ${e.message}", e)
        }

        // 2. Fallback de contingencia local si no hay conexión
        val year = Calendar.getInstance().get(Calendar.YEAR)
        sequenceCounter++
        val generatedCode = String.format("DON-%d-%05d", year, sequenceCounter)

        val localDonation = donation.copy(
            id = sequenceCounter,
            donationCode = generatedCode
        )

        memoryDonations.add(localDonation)
        Log.w(TAG, "Donación guardada localmente por modo contingencia: $generatedCode")
        return@withContext localDonation
    }

    suspend fun getDonations(): List<Donation> = withContext(Dispatchers.IO) {
        memoryDonations.toList()
    }
}
