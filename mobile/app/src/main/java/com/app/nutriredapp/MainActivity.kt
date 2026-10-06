package com.app.nutriredapp

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.navigation3.runtime.NavEntry
import androidx.navigation3.runtime.rememberNavBackStack
import androidx.navigation3.ui.NavDisplay
import com.app.nutriredapp.navigation.NavRoute
import com.app.nutriredapp.ui.auth.AuthScreen
import com.app.nutriredapp.ui.home.HomeScreen
import com.app.nutriredapp.ui.theme.NutriRedAppTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            NutriRedAppTheme {
                NutriRedApp()
            }
        }
    }
}

@Composable
fun NutriRedApp() {
    val backStack = rememberNavBackStack(NavRoute.Auth)

    NavDisplay(
        backStack = backStack,
        modifier = Modifier.fillMaxSize(),
        onBack = { backStack.removeLastOrNull() },
        entryProvider = { key ->
            when (key) {
                is NavRoute.Auth -> {
                    NavEntry(key) {
                        AuthScreen(
                            onLoginSuccess = {
                                backStack.clear()
                                backStack.add(NavRoute.Home)
                            }
                        )
                    }
                }
                is NavRoute.Home -> {
                    NavEntry(key) {
                        HomeScreen(
                            onLogout = {
                                backStack.clear()
                                backStack.add(NavRoute.Auth)
                            },
                            onNavigateToDonation = {
                                backStack.add(NavRoute.DonationReception)
                            },
                            onNavigateToDelivery = {
                                backStack.add(NavRoute.DeliveryDispatch)
                            }
                        )
                    }
                }
                is NavRoute.DonationReception -> {
                    NavEntry(key) {
                        com.app.nutriredapp.ui.donation.DonationFlowScreen(
                            onBackToHome = { backStack.removeLastOrNull() }
                        )
                    }
                }
                is NavRoute.DeliveryDispatch -> {
                    NavEntry(key) {
                        com.app.nutriredapp.ui.delivery.DeliveryDispatchScreen(
                            onBackToHome = { backStack.removeLastOrNull() }
                        )
                    }
                }
                else -> error("Unknown route: $key")
            }
        }
    )
}
