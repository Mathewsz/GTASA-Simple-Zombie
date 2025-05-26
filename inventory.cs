// CLEO Script: Inventory System
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
VAR
    $player_food_items: int = 0
    $player_water_items: int = 0
    $inventory_pickups_initialized: bool = false // Flag for one-time pickup creation
END

VAR // Pickup Handles - need to be accessible by monitor thread
    $food_pickup_handles[3]: PICKUP_HANDLE
    $water_pickup_handles[3]: PICKUP_HANDLE
END

CONST
    FOOD_MODEL: int = 1240 // Generic item box model
    WATER_MODEL: int = 1242 // Adrenaline pill bottle model
    PICKUP_TYPE_RESPAWN: int = 7 // Disappears and reappears

    FOOD_RESTORE_VALUE: float = 50.0
    WATER_RESTORE_VALUE: float = 50.0

    // Audio IDs for consuming items
    AUDIO_EAT: int = 1057
    AUDIO_DRINK: int = 1058
END

// External Global Variables
VAR_EXTERNAL
    $player_hunger: float             // From survival.cs
    $player_thirst: float             // From survival.cs
    $survival_mechanics_active: bool // From main_menu.cs
END

//----------------------------------------------------------------------------------
// --- PICKUP INITIALIZATION THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "INV_PICKUPS_INIT"

IF NOT $inventory_pickups_initialized
THEN
    // Food Pickups - Grove Street Area
    032B: create_pickup FOOD_MODEL type PICKUP_TYPE_RESPAWN at 2495.0 -1670.0 13.5 store_to $food_pickup_handles[0] // Near CJ's house
    032B: create_pickup FOOD_MODEL type PICKUP_TYPE_RESPAWN at 2470.0 -1680.0 13.5 store_to $food_pickup_handles[1] // Alley
    032B: create_pickup FOOD_MODEL type PICKUP_TYPE_RESPAWN at 2510.0 -1690.0 13.5 store_to $food_pickup_handles[2] // Another spot

    // Water Pickups - Grove Street Area
    032B: create_pickup WATER_MODEL type PICKUP_TYPE_RESPAWN at 2490.0 -1700.0 13.5 store_to $water_pickup_handles[0] // Near wall
    032B: create_pickup WATER_MODEL type PICKUP_TYPE_RESPAWN at 2515.0 -1675.0 13.5 store_to $water_pickup_handles[1] // Another spot
    032B: create_pickup WATER_MODEL type PICKUP_TYPE_RESPAWN at 2475.0 -1650.0 13.5 store_to $water_pickup_handles[2] // Near bridge/canal

    $inventory_pickups_initialized = true
    0ACD: show_text_highpriority "Inventory Pickups Created" time 2000 // Optional: Debug
END

// This thread's main purpose is one-time initialization. It can terminate.
0A93: terminate_this_custom_thread


//----------------------------------------------------------------------------------
// --- PICKUP MONITORING THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "INV_PICKUP_MONITOR"

WHILE true
    WAIT 500 // Check for pickup collections every 500ms (adjust as needed)

    IF $survival_mechanics_active // Only monitor pickups if survival mechanics are active
    THEN
        IF $inventory_pickups_initialized // Only monitor if pickups were created
        THEN
            // Monitor Food Pickups
            FOR $i = 0 TO 2
                IF 02A7: has_pickup_been_collected $food_pickup_handles[$i]
                THEN
                    $player_food_items += 1
                    0ACD: show_text_highpriority_styled "PEGOU COMIDA" time 2000 style 2
                END
            ENDFOR

            // Monitor Water Pickups
            FOR $i = 0 TO 2
                IF 02A7: has_pickup_been_collected $water_pickup_handles[$i]
                THEN
                    $player_water_items += 1
                    0ACD: show_text_highpriority_styled "PEGOU ÁGUA" time 2000 style 2
                END
            ENDFOR
        ELSE
            WAIT 2000 // If not initialized, wait longer before checking again
        END
    ELSE
        WAIT 1000 // If survival is off, check less frequently or simply do nothing active
    END
END


//----------------------------------------------------------------------------------
// --- INVENTORY MENU THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "INV_MENU_CHECK"

WHILE true
    WAIT 0 // Check for key press every frame

    IF $survival_mechanics_active // Only allow inventory menu if survival mechanics are active
    THEN
        IF 00D6: if 0AB0: key_just_pressed 0x49 // Check for 'I' key (VK_KEY_I)
        THEN
            // 0ACD: show_text_highpriority "DEBUG: I key pressed" time 1000 // Optional: Debug

            // Pause game for menu (optional, but common for menus)
            // 0AE1: set_game_state_paused 1

        // Show Menu
        0A8D: show_menu_with_title "INVENTORY_MENU_TITLE" (Inventário) current_item 0

        // Add Items to Menu
        // Using direct strings for menu items as GXT setup is not part of this task
        0A8E: add_item_to_menu "INVENTORY_MENU" item_key "INV_USE_FOOD" text "Usar Comida (Tens: %d)" params_count 1 $player_food_items
        0A8E: add_item_to_menu "INVENTORY_MENU" item_key "INV_USE_WATER" text "Usar Água (Tens: %d)" params_count 1 $player_water_items
        0A8E: add_item_to_menu "INVENTORY_MENU" item_key "INV_CLOSE" text "Fechar" params_count 0

        // Wait for player selection
        WHILE true
            WAIT 0 // Process menu input
            IF 0A91: is_menu_active "INVENTORY_MENU"
            THEN
                IF 0A92: $SELECTED_ITEM = get_menu_item_selected "INVENTORY_MENU"
                THEN
                    // Process selected item
                    IF $SELECTED_ITEM == 0 // Use Food
                    THEN
                        IF $player_food_items > 0
                        THEN
                            $player_food_items -= 1
                            $player_hunger += FOOD_RESTORE_VALUE
                            008C: $player_hunger = min $player_hunger 100.0 // Clamp hunger
                            03CF: play_audio_at_player $PLAYER_CHAR audio_id AUDIO_EAT
                            0ACD: show_text_highpriority "Comeu algo." time 2000
                        ELSE
                            0ACD: show_text_highpriority "Não tens comida!" time 2000
                        END
                        BREAK // Exit selection loop, menu will be destroyed
                    ELSIF $SELECTED_ITEM == 1 // Use Water
                    THEN
                        IF $player_water_items > 0
                        THEN
                            $player_water_items -= 1
                            $player_thirst += WATER_RESTORE_VALUE
                            008C: $player_thirst = min $player_thirst 100.0 // Clamp thirst
                            03CF: play_audio_at_player $PLAYER_CHAR audio_id AUDIO_DRINK
                            0ACD: show_text_highpriority "Bebeu água." time 2000
                        ELSE
                            0ACD: show_text_highpriority "Não tens água!" time 2000
                        END
                        BREAK // Exit selection loop
                    ELSIF $SELECTED_ITEM == 2 // Close
                    THEN
                        BREAK // Exit selection loop
                    END
                END
            ELSE // Menu is no longer active (e.g., closed with Esc)
                BREAK // Exit selection loop
            END
        ENDWHILE

        // Destroy Menu
        0A8F: destroy_menu "INVENTORY_MENU"

        // Unpause game if it was paused
        // 0AE1: set_game_state_paused 0

        WAIT 500 // Debounce after closing menu to prevent immediate re-opening
    END
END
