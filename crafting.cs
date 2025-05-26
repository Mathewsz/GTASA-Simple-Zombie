// CLEO Script: Crafting System
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
VAR
    $player_wood_planks: int = 0
    $player_scrap_metal: int = 0 // Defined, but not used in this phase
    $player_cloth_rags: int = 0
    $player_empty_bottles: int = 0
    $player_nails: int = 0
    $player_barricade_kits: int = 0 // For crafted barricades

    $craft_pickups_initialized: bool = false // Flag for one-time pickup creation
END

VAR // Pickup Handles
    $wood_pickup_handles[5]: PICKUP_HANDLE
    $cloth_pickup_handles[5]: PICKUP_HANDLE
    $bottle_pickup_handles[5]: PICKUP_HANDLE
    $nail_pickup_handles[5]: PICKUP_HANDLE
END

CONST
    WOOD_PLANK_MODEL: int = 1271
    CLOTH_RAG_MODEL: int = 1273  // Money bag model as placeholder
    EMPTY_BOTTLE_MODEL: int = 1550
    NAIL_MODEL: int = 1240       // Briefcase model as placeholder

    PICKUP_TYPE_RESPAWN: int = 7

    // Audio ID for successful craft (e.g., weapon pickup sound)
    AUDIO_CRAFT_SUCCESS: int = 1084
END

//----------------------------------------------------------------------------------
// --- PICKUP INITIALIZATION THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "CRAFT_PICKUPS_INIT"

IF NOT $craft_pickups_initialized
THEN
    // Wood Pickups
    032B: create_pickup WOOD_PLANK_MODEL type PICKUP_TYPE_RESPAWN at 2800.0 -2400.0 14.0 store_to $wood_pickup_handles[0] // Ocean Docks
    032B: create_pickup WOOD_PLANK_MODEL type PICKUP_TYPE_RESPAWN at -50.0 1200.0 28.0 store_to $wood_pickup_handles[1]    // Blueberry Acres
    032B: create_pickup WOOD_PLANK_MODEL type PICKUP_TYPE_RESPAWN at 2050.0 -1200.0 24.0 store_to $wood_pickup_handles[2] // LS Construction Placeholder
    032B: create_pickup WOOD_PLANK_MODEL type PICKUP_TYPE_RESPAWN at -1500.0 600.0 7.0 store_to $wood_pickup_handles[3]   // Dillimore
    032B: create_pickup WOOD_PLANK_MODEL type PICKUP_TYPE_RESPAWN at -2200.0 -300.0 35.0 store_to $wood_pickup_handles[4]  // Whetstone

    // Cloth Rag Pickups
    032B: create_pickup CLOTH_RAG_MODEL type PICKUP_TYPE_RESPAWN at 2450.0 -1650.0 13.5 store_to $cloth_pickup_handles[0] // Ganton
    032B: create_pickup CLOTH_RAG_MODEL type PICKUP_TYPE_RESPAWN at 2200.0 -1800.0 13.5 store_to $cloth_pickup_handles[1] // Idlewood
    032B: create_pickup CLOTH_RAG_MODEL type PICKUP_TYPE_RESPAWN at 1800.0 -1900.0 13.5 store_to $cloth_pickup_handles[2] // El Corona
    032B: create_pickup CLOTH_RAG_MODEL type PICKUP_TYPE_RESPAWN at 2505.0 -1685.0 13.5 store_to $cloth_pickup_handles[3] // Binco Grove St
    032B: create_pickup CLOTH_RAG_MODEL type PICKUP_TYPE_RESPAWN at 1650.0 -1600.0 13.0 store_to $cloth_pickup_handles[4] // Unity Station

    // Empty Bottle Pickups
    032B: create_pickup EMPTY_BOTTLE_MODEL type PICKUP_TYPE_RESPAWN at 1500.0 -1400.0 14.0 store_to $bottle_pickup_handles[0] // Downtown LS Bar
    032B: create_pickup EMPTY_BOTTLE_MODEL type PICKUP_TYPE_RESPAWN at 2600.0 -1300.0 24.0 store_to $bottle_pickup_handles[1] // East LS Alley
    032B: create_pickup EMPTY_BOTTLE_MODEL type PICKUP_TYPE_RESPAWN at 800.0 -1950.0 13.0 store_to $bottle_pickup_handles[2]  // Verona Beach
    032B: create_pickup EMPTY_BOTTLE_MODEL type PICKUP_TYPE_RESPAWN at 2550.0 -1660.0 13.5 store_to $bottle_pickup_handles[3] // Ten Green Bottles Bar
    032B: create_pickup EMPTY_BOTTLE_MODEL type PICKUP_TYPE_RESPAWN at 2300.0 -1500.0 17.0 store_to $bottle_pickup_handles[4] // Grocery Store Alley

    // Nail Pickups
    032B: create_pickup NAIL_MODEL type PICKUP_TYPE_RESPAWN at 1300.0 -1200.0 13.5 store_to $nail_pickup_handles[0] // Hardware Store Downtown
    032B: create_pickup NAIL_MODEL type PICKUP_TYPE_RESPAWN at -2000.0 -150.0 30.0 store_to $nail_pickup_handles[1] // Doherty Garage SF
    032B: create_pickup NAIL_MODEL type PICKUP_TYPE_RESPAWN at 2055.0 -1205.0 24.0 store_to $nail_pickup_handles[2] // LS Construction Site
    032B: create_pickup NAIL_MODEL type PICKUP_TYPE_RESPAWN at 2750.0 -2350.0 14.0 store_to $nail_pickup_handles[3] // Ocean Docks Warehouse
    032B: create_pickup NAIL_MODEL type PICKUP_TYPE_RESPAWN at -300.0 800.0 20.0 store_to $nail_pickup_handles[4]  // Flint County Industrial

    $craft_pickups_initialized = true
    0ACD: show_text_highpriority "Crafting Pickups Created" time 2000 // Optional: Debug
END

0A93: terminate_this_custom_thread // End of one-time init thread

//----------------------------------------------------------------------------------
// --- PICKUP MONITORING THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "CRAFT_PICKUP_MONITOR"

WHILE true
    WAIT 500 // Check for pickup collections every 500ms

    IF $craft_pickups_initialized
    THEN
        // Monitor Wood Planks
        FOR $i = 0 TO 4
            IF 02A7: has_pickup_been_collected $wood_pickup_handles[$i]
            THEN
                $player_wood_planks += 1
                0ACD: show_text_highpriority_styled "PEGOU TÁBUAS DE MADEIRA" time 2000 style 2
            END
        ENDFOR

        // Monitor Cloth Rags
        FOR $i = 0 TO 4
            IF 02A7: has_pickup_been_collected $cloth_pickup_handles[$i]
            THEN
                $player_cloth_rags += 1
                0ACD: show_text_highpriority_styled "PEGOU RETALHOS DE PANO" time 2000 style 2
            END
        ENDFOR

        // Monitor Empty Bottles
        FOR $i = 0 TO 4
            IF 02A7: has_pickup_been_collected $bottle_pickup_handles[$i]
            THEN
                $player_empty_bottles += 1
                0ACD: show_text_highpriority_styled "PEGOU GARRAFA VAZIA" time 2000 style 2
            END
        ENDFOR

        // Monitor Nails
        FOR $i = 0 TO 4
            IF 02A7: has_pickup_been_collected $nail_pickup_handles[$i]
            THEN
                $player_nails += 1
                0ACD: show_text_highpriority_styled "PEGOU PREGOS" time 2000 style 2
            END
        ENDFOR
    ELSE
        WAIT 2000 // If not initialized, wait longer before checking again
    END
END

//----------------------------------------------------------------------------------
// --- CRAFTING MENU THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "CRAFT_MENU_CHECK"

WHILE true
    WAIT 0 // Check for key press every frame

    IF 00D6: if 0AB0: key_just_pressed 0x43 // Check for 'C' key (VK_KEY_C)
    THEN
        // Show Menu
        0A8D: show_menu_with_title "CRAFTING_MENU_TITLE" (Criar Itens) current_item 0

        // Add Items to Menu
        0A8E: add_item_to_menu "CRAFTING_MENU" item_key "CRAFT_BARRICADE" text "Barricada Portátil (Madeira:2 Pregos:4)" params_count 0
        0A8E: add_item_to_menu "CRAFTING_MENU" item_key "CRAFT_MOLOTOV" text "Coquetel Molotov (Garrafa:1 Pano:1)" params_count 0
        0A8E: add_item_to_menu "CRAFTING_MENU" item_key "CRAFT_NAIL_BAT" text "Taco com Pregos (Madeira:3 Pregos:10)" params_count 0
        0A8E: add_item_to_menu "CRAFTING_MENU" item_key "CRAFT_CLOSE" text "Fechar" params_count 0

        // Wait for player selection
        WHILE true
            WAIT 0 // Process menu input
            IF 0A91: is_menu_active "CRAFTING_MENU"
            THEN
                IF 0A92: $SELECTED_ITEM = get_menu_item_selected "CRAFTING_MENU"
                THEN
                    // Process selected item
                    IF $SELECTED_ITEM == 0 // Barricada Portátil
                    THEN
                        IF $player_wood_planks >= 2 AND $player_nails >= 4
                        THEN
                            $player_wood_planks -= 2
                            $player_nails -= 4
                            $player_barricade_kits += 1
                            03CF: play_audio_at_player $PLAYER_CHAR audio_id AUDIO_CRAFT_SUCCESS
                            0ACD: show_text_highpriority "Kit de Barricada criado!" time 2000
                        ELSE
                            0ACD: show_text_highpriority "Componentes insuficientes!" time 2000
                        END
                        BREAK // Exit selection loop
                    ELSIF $SELECTED_ITEM == 1 // Coquetel Molotov
                    THEN
                        IF $player_empty_bottles >= 1 AND $player_cloth_rags >= 1
                        THEN
                            $player_empty_bottles -= 1
                            $player_cloth_rags -= 1
                            01B3: give_player $PLAYER_CHAR weapon 18 ammo 1 // Weapon 18 = Molotov
                            03CF: play_audio_at_player $PLAYER_CHAR audio_id AUDIO_CRAFT_SUCCESS
                            0ACD: show_text_highpriority "Coquetel Molotov criado!" time 2000
                        ELSE
                            0ACD: show_text_highpriority "Componentes insuficientes!" time 2000
                        END
                        BREAK // Exit selection loop
                    ELSIF $SELECTED_ITEM == 2 // Taco com Pregos
                    THEN
                        IF $player_wood_planks >= 3 AND $player_nails >= 10
                        THEN
                            $player_wood_planks -= 3
                            $player_nails -= 10
                            01B3: give_player $PLAYER_CHAR weapon 5 ammo 1 // Weapon 5 = Baseball Bat
                            03CF: play_audio_at_player $PLAYER_CHAR audio_id AUDIO_CRAFT_SUCCESS
                            0ACD: show_text_highpriority "Taco com Pregos criado!" time 2000
                        ELSE
                            0ACD: show_text_highpriority "Componentes insuficientes!" time 2000
                        END
                        BREAK // Exit selection loop
                    ELSIF $SELECTED_ITEM == 3 // Fechar
                    THEN
                        BREAK // Exit selection loop
                    END
                END
            ELSE // Menu is no longer active (e.g., closed with Esc)
                BREAK // Exit selection loop
            END
        ENDWHILE

        // Destroy Menu
        0A8F: destroy_menu "CRAFTING_MENU"
        WAIT 500 // Debounce after closing menu
    END
END
