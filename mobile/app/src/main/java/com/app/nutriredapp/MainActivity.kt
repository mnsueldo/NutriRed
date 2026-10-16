package com.app.nutriredapp

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Remove
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.navigation3.runtime.NavEntry
import androidx.navigation3.runtime.rememberNavBackStack
import androidx.navigation3.ui.NavDisplay
import androidx.compose.foundation.isSystemInDarkTheme
import com.app.nutriredapp.navigation.NavRoute
import com.app.nutriredapp.ui.auth.AuthScreen
import com.app.nutriredapp.ui.home.HomeScreen
import com.app.nutriredapp.ui.splash.SplashScreen
import com.app.nutriredapp.ui.theme.NutriRedAppTheme
import com.app.nutriredapp.ui.theme.ThemeManager

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        val splashScreen = installSplashScreen()
        super.onCreate(savedInstanceState)
        com.app.nutriredapp.data.network.NutriRedApiClient.init(this)
        enableEdgeToEdge()
        setContent {
            var fontScaleMultiplier by remember { mutableFloatStateOf(1f) }
            val currentDensity = LocalDensity.current
            val customDensity = Density(
                density = currentDensity.density,
                fontScale = currentDensity.fontScale * fontScaleMultiplier
            )

            val systemDark = isSystemInDarkTheme()
            val customDark by ThemeManager.isDarkTheme.collectAsState()
            val isDarkTheme = customDark ?: systemDark

            NutriRedAppTheme(darkTheme = isDarkTheme) {
                CompositionLocalProvider(LocalDensity provides customDensity) {
                    Surface(
                        modifier = Modifier.fillMaxSize(),
                        color = MaterialTheme.colorScheme.background
                    ) {
                        Box(modifier = Modifier.fillMaxSize()) {
                            NutriRedApp()

                            // Controles de accesibilidad flotantes (aumento y reducción de fuente WCAG)
                            Row(
                                modifier = Modifier
                                    .align(Alignment.BottomStart)
                                    .padding(start = 16.dp, bottom = 48.dp)
                            ) {
                                SmallFloatingActionButton(
                                    onClick = { if (fontScaleMultiplier > 0.8f) fontScaleMultiplier -= 0.1f },
                                    shape = CircleShape,
                                    containerColor = MaterialTheme.colorScheme.secondary
                                ) {
                                    Icon(Icons.Rounded.Remove, contentDescription = "Reducir tamaño de texto")
                                }
                                Spacer(modifier = Modifier.width(8.dp))
                                SmallFloatingActionButton(
                                    onClick = { if (fontScaleMultiplier < 1.6f) fontScaleMultiplier += 0.1f },
                                    shape = CircleShape,
                                    containerColor = MaterialTheme.colorScheme.secondary
                                ) {
                                    Icon(Icons.Rounded.Add, contentDescription = "Aumentar tamaño de texto")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun NutriRedApp() {
    val backStack = rememberNavBackStack(NavRoute.Splash)

    NavDisplay(
        backStack = backStack,
        modifier = Modifier.fillMaxSize(),
        onBack = { backStack.removeLastOrNull() },
        entryProvider = { key ->
            when (key) {
                is NavRoute.Splash -> {
                    NavEntry(key) {
                        SplashScreen(
                            onSplashFinished = {
                                backStack.clear()
                                backStack.add(NavRoute.Auth)
                            }
                        )
                    }
                }
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
