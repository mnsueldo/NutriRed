package com.app.nutriredapp.navigation

import androidx.navigation3.runtime.NavKey
import kotlinx.serialization.Serializable

sealed interface NavRoute : NavKey {
    @Serializable
    data object Auth : NavRoute

    @Serializable
    data object Home : NavRoute

    @Serializable
    data object DonationReception : NavRoute

    @Serializable
    data object DeliveryDispatch : NavRoute
}
