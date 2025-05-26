// CLEO Script: Survival - Hunger and Thirst
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
VAR
    $player_hunger: float = 100.0
    $player_thirst: float = 100.0
    $survival_initialized: bool = false // Flag to check if script has been initialized once per session

    // Timers for health loss
    $last_hunger_damage_time: int = 0
    $last_thirst_damage_time: int = 0
END

// External Global Variables from main_menu.cs
VAR_EXTERNAL
    $survival_mechanics_active: bool // To enable/disable survival features
END

CONST
    HUNGER_REDUCTION_INTERVAL: int = 300000 // 5 minutes (300000 ms)
    THIRST_REDUCTION_INTERVAL: int = 300000 // 5 minutes (300000 ms) - Same as hunger for simplicity, can be different

    HUNGER_REDUCTION_AMOUNT: float = 5.0
    THIRST_REDUCTION_AMOUNT: float = 7.5

    HEALTH_LOSS_HUNGER_INTERVAL: int = 30000 // 30 seconds
    HEALTH_LOSS_THIRST_INTERVAL: int = 20000 // 20 seconds
    HEALTH_LOSS_AMOUNT: int = 1
END

//----------------------------------------------------------------------------------
// --- INITIALIZATION & MAIN LOGIC THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "SURV_MAIN"

// --- Initialization Logic (runs once) ---
IF NOT $survival_initialized
THEN
    $player_hunger = 100.0
    $player_thirst = 100.0
    0005: $last_hunger_damage_time = game_timer_in_ms // Initialize damage timers
    0005: $last_thirst_damage_time = game_timer_in_ms
    $survival_initialized = true
    0ACD: show_text_highpriority "Survival Script Initialized" time 2000 // Optional: Debug message
END

// --- Main Loop for Hunger/Thirst Reduction ---
WHILE true
    WAIT HUNGER_REDUCTION_INTERVAL // Wait for the defined interval (e.g., 5 minutes)

    IF $survival_mechanics_active // Check if survival mechanics are enabled from main menu
    THEN
        IF $survival_initialized // Ensure this only runs after initialization
        THEN
            IF 03E3: not is_game_paused
            THEN
                // Reduce Hunger
                $player_hunger -= HUNGER_REDUCTION_AMOUNT
                008B: $player_hunger = max $player_hunger 0.0 // Ensure hunger doesn't go below 0

                // Reduce Thirst
                $player_thirst -= THIRST_REDUCTION_AMOUNT
                008B: $player_thirst = max $player_thirst 0.0 // Ensure thirst doesn't go below 0

                // 0ACD: show_text_highpriority "DEBUG: Hunger/Thirst Reduced" time 1000 // Optional: Debug message
            END
        END
    END
END // End of Main Loop

//----------------------------------------------------------------------------------
// --- HEALTH EFFECTS THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "SURV_HEALTH"

WHILE true
    WAIT 1000 // Check every second (adjust as needed for performance vs responsiveness)

    IF $survival_mechanics_active // Check if survival mechanics are enabled
    THEN
        IF $survival_initialized
        THEN
            IF 03E3: not is_game_paused
            THEN
                VAR $current_time_ms: int
                0005: $current_time_ms = game_timer_in_ms
            VAR $player_health_current: int

            // --- Hunger Health Effect ---
            IF $player_hunger <= 0.0
            THEN
                IF $current_time_ms - $last_hunger_damage_time > HEALTH_LOSS_HUNGER_INTERVAL
                THEN
                    00E1: $player_health_current = player $PLAYER_CHAR health
                    IF $player_health_current > 0 // Only apply damage if alive
                    THEN
                        $player_health_current -= HEALTH_LOSS_AMOUNT
                        008B: $player_health_current = max $player_health_current 0 // Prevent health going into negative from script
                        0229: set_player $PLAYER_CHAR health $player_health_current
                        $last_hunger_damage_time = $current_time_ms
                        // 0ACD: show_text_highpriority "HP lost - Hunger" time 1000 // Optional: Debug
                    END
                END
            END

            // --- Thirst Health Effect ---
            IF $player_thirst <= 0.0
            THEN
                IF $current_time_ms - $last_thirst_damage_time > HEALTH_LOSS_THIRST_INTERVAL
                THEN
                    00E1: $player_health_current = player $PLAYER_CHAR health
                    IF $player_health_current > 0 // Only apply damage if alive
                    THEN
                        $player_health_current -= HEALTH_LOSS_AMOUNT
                        008B: $player_health_current = max $player_health_current 0
                        0229: set_player $PLAYER_CHAR health $player_health_current
                        $last_thirst_damage_time = $current_time_ms
                        // 0ACD: show_text_highpriority "HP lost - Thirst" time 1000 // Optional: Debug
                    END
                END
            END
        END
    END
END // End of Health Effects Loop

//----------------------------------------------------------------------------------
// --- HUD DISPLAY THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "SURV_HUD"

WHILE true
    WAIT 0 // Run every frame for smooth HUD

    IF $survival_mechanics_active // Check if survival mechanics are enabled
    THEN
        IF $survival_initialized
        THEN
            // Check if player is alive and on screen to display HUD
            IF 00DF:   actor $PLAYER_ACTOR driving 
            OR 80DF: not actor $PLAYER_ACTOR driving 
            THEN
                // --- Set Text Style ---
                03F1: set_text_draw_font 1          // Font style
                03F2: set_text_draw_color 0xFFFFFFFF // Color White (ARGB)
            03F3: set_text_draw_centered 0      // Align left
            03F4: set_text_draw_proportional 1  // Proportional text
            03F5: set_text_draw_shadow 1 color 0xFF000000 // Shadow black
            080E: set_text_draw_letter_size 0.5 1.2 // Letter size (width, height)

            // --- Display Hunger ---
            // Format: "FOME: 100%"
            03F0: text_high_priority_draw text "FOME: %.0f%%" args 1 $player_hunger at 10 300
            
            // --- Display Thirst ---
            // Format: "SEDE: 100%"
            03F0: text_high_priority_draw text "SEDE: %.0f%%" args 1 $player_thirst at 10 320
        END
    END
END // End of HUD Loop
