package com.app.nutriredapp.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColorScheme = darkColorScheme(
    primary = HighContrastDarkPrimary,
    secondary = HighContrastDarkSecondary,
    background = HighContrastDarkBackground,
    surface = HighContrastDarkSurface,
    surfaceVariant = Color(0xFF262626),
    onPrimary = Color.Black,
    onSecondary = Color.Black,
    onBackground = HighContrastDarkText,
    onSurface = HighContrastDarkText,
    onSurfaceVariant = Color(0xFFD6D6D6),
    primaryContainer = Color(0xFF1E3A5F),
    onPrimaryContainer = Color(0xFFFFFFFF),
    outline = Color(0xFF888888)
)

private val LightColorScheme = lightColorScheme(
    primary = HighContrastPrimary,
    secondary = HighContrastSecondary,
    background = HighContrastBackground,
    surface = HighContrastSurface,
    surfaceVariant = Color(0xFFEEEEEE),
    onPrimary = Color.White,
    onSecondary = Color.White,
    onBackground = HighContrastText,
    onSurface = HighContrastText,
    onSurfaceVariant = Color(0xFF333333),
    primaryContainer = Color(0xFF003366),
    onPrimaryContainer = Color(0xFFFFFFFF),
    outline = Color(0xFF555555)
)

@Composable
fun NutriRedAppTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    // Se deshabilita el color dinámico para asegurar que se mantenga el alto contraste
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}