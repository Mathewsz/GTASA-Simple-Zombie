// CLEO Script: Main Menu for SA-ZA Mod
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
CONST
    MENU_KEY: int = 0x79 // F10
    // Default Zombie Settings (Normal)
    ZOMBIE_MAX_NORMAL: int = 20
    ZOMBIE_HEALTH_NORMAL: int = 50
    // Hard Zombie Settings
    ZOMBIE_MAX_HARD: int = 30
    ZOMBIE_HEALTH_HARD: int = 75

    MENU_TITLE_MAIN: string = "SAZA_MAIN_MENU" // Internal name for main menu
    MENU_TITLE_NPC: string = "SAZA_NPC_MENU"   // Internal name for NPC submenu
END

// External Global Variables (must be defined in other scripts)
VAR_EXTERNAL
    $zombie_mode_active: bool         // from zombie_spawn.cs
    $max_zombies: int                 // from zombie_spawn.cs
    $ZOMBIE_HEALTH: int               // from zombie_spawn.cs (Note: case sensitivity, ensure matches)
    $squad_current_order: int         // from npc_recruitment.cs
    $recruited_npcs_count: int        // from npc_recruitment.cs
END

// New Global Variables for this script
VAR
    $survival_mechanics_active: bool = true // Default to active
    $zombie_difficulty: int = 0           // 0=Normal, 1=Hard (Default to Normal)
    $trigger_save_bases: bool = false
    $trigger_dismiss_all_npcs: bool = false
    $main_menu_active_flag: bool = false // To prevent multiple menu instances
END

// Temporary string variables for dynamic menu text
VAR
    $MenuItemText1: string[128]
    $MenuItemText2: string[128]
    $MenuItemText3: string[128]
END

//----------------------------------------------------------------------------------
// --- KEY CHECK THREAD (To open main menu) ---
//----------------------------------------------------------------------------------
03A4: name_thread "MM_KEY_CHECK"

WHILE true
    WAIT 0 // Check every frame

    IF 00D6: if 0AB0: key_just_pressed MENU_KEY
    THEN
        IF NOT $main_menu_active_flag // Only open if not already active
        THEN
            $main_menu_active_flag = true
            0ACD: show_text_highpriority "Abrindo Menu..." time 500 // Feedback
            0A9C: start_custom_thread_named "MM_SHOW_MAIN_MENU" // Start the main menu display thread
        END
        WAIT 300 // Debounce key press
    END
END

//----------------------------------------------------------------------------------
// --- MAIN MENU DISPLAY THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "MM_SHOW_MAIN_MENU"

VAR $selected_main_menu_item: int

WHILE true // Loop to redraw menu until "Close" or submenu navigation
    WAIT 0

    // Prepare dynamic text for menu items
    // 1. Spawn de Zumbis
    IF $zombie_mode_active
    THEN
        0C1E: sprintf $MenuItemText1 "Spawn de Zumbis: ATIVADO"
    ELSE
        0C1E: sprintf $MenuItemText1 "Spawn de Zumbis: DESATIVADO"
    END

    // 2. Sobrevivência
    IF $survival_mechanics_active
    THEN
        0C1E: sprintf $MenuItemText2 "Sobrevivencia: ATIVADO"
    ELSE
        0C1E: sprintf $MenuItemText2 "Sobrevivencia: DESATIVADO"
    END

    // 3. Dificuldade Zumbis
    IF $zombie_difficulty == 0 // Normal
    THEN
        0C1E: sprintf $MenuItemText3 "Dificuldade Zumbis: NORMAL"
    ELSE // Hard
        0C1E: sprintf $MenuItemText3 "Dificuldade Zumbis: DIFICIL"
    END

    // Show Main Menu
    0A8D: show_menu_with_title MENU_TITLE_MAIN (Menu SA-ZA) current_item 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_TOGGLE_ZOMBIES" text $MenuItemText1 params_count 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_TOGGLE_SURVIVAL" text $MenuItemText2 params_count 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_TOGGLE_DIFFICULTY" text $MenuItemText3 params_count 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_SAVE_BASES" text "Salvar Posicao das Bases" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_NPC_SUBMENU" text "Comandos de Esquadrao NPCs" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_MAIN item_key "MM_CLOSE" text "Fechar Menu" params_count 0

    IF 0A91: is_menu_active MENU_TITLE_MAIN
    THEN
        IF 0A92: $selected_main_menu_item = get_menu_item_selected MENU_TITLE_MAIN
        THEN
            // Process Selection
            IF $selected_main_menu_item == 0 // Toggle Zombies
            THEN
                $zombie_mode_active = 1 - $zombie_mode_active
                // Menu will redraw with updated text
            ELSIF $selected_main_menu_item == 1 // Toggle Survival
            THEN
                $survival_mechanics_active = 1 - $survival_mechanics_active
            ELSIF $selected_main_menu_item == 2 // Toggle Difficulty
            THEN
                $zombie_difficulty = 1 - $zombie_difficulty
                IF $zombie_difficulty == 0 // Normal
                THEN
                    $max_zombies = ZOMBIE_MAX_NORMAL
                    $ZOMBIE_HEALTH = ZOMBIE_HEALTH_NORMAL
                    0ACD: show_text_highpriority "Dificuldade: NORMAL" time 1500
                ELSE // Hard
                    $max_zombies = ZOMBIE_MAX_HARD
                    $ZOMBIE_HEALTH = ZOMBIE_HEALTH_HARD
                    0ACD: show_text_highpriority "Dificuldade: DIFICIL" time 1500
                END
            ELSIF $selected_main_menu_item == 3 // Save Bases
            THEN
                $trigger_save_bases = true
                0ACD: show_text_highpriority "Sinal para salvar bases enviado!" time 2000
            ELSIF $selected_main_menu_item == 4 // NPC Submenu
            THEN
                0A8F: destroy_menu MENU_TITLE_MAIN // Close current menu
                0A9C: start_custom_thread_named "MM_SHOW_NPC_SUBMENU" // Open submenu
                $main_menu_active_flag = false // Allow MM_KEY_CHECK to open main menu again later
                0A93: terminate_this_custom_thread // End this instance of main menu
                BREAK // Exit loop just in case
            ELSIF $selected_main_menu_item == 5 // Close Menu
            THEN
                0A8F: destroy_menu MENU_TITLE_MAIN
                $main_menu_active_flag = false
                0A93: terminate_this_custom_thread
                BREAK // Exit loop
            END
            WAIT 200 // Debounce selection to allow text to update if menu isn't closed
        END
    ELSE // Menu is no longer active (e.g., closed with Esc)
        $main_menu_active_flag = false
        0A93: terminate_this_custom_thread // Ensure thread ends if menu closed externally
        BREAK
    END
ENDWHILE
0A93: terminate_this_custom_thread // Fallback termination

//----------------------------------------------------------------------------------
// --- NPC SUBMENU DISPLAY THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "MM_SHOW_NPC_SUBMENU"
$main_menu_active_flag = true // Keep flag true while in submenu

VAR $selected_npc_menu_item: int
VAR $SquadOrderText: string[128]
VAR $NPCCountText: string[128]

WHILE true // Loop to redraw submenu
    WAIT 0

    // Prepare dynamic text for NPC submenu (optional, but good for info)
    IF $squad_current_order == 1
    THEN
        0C1E: sprintf $SquadOrderText "Ordem Atual: Seguindo"
    ELSIF $squad_current_order == 2
    THEN
        0C1E: sprintf $SquadOrderText "Ordem Atual: Parado"
    ELSE
        0C1E: sprintf $SquadOrderText "Ordem Atual: Defender Area"
    END
    0C1E: sprintf $NPCCountText "Recrutas Ativos: %d" $recruited_npcs_count

    // Show NPC Submenu
    0A8D: show_menu_with_title MENU_TITLE_NPC (Comandos de Esquadrao) current_item 0
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_INFO_ORDER" text $SquadOrderText params_count 0 // Non-selectable info
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_INFO_COUNT" text $NPCCountText params_count 0 // Non-selectable info
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_ORDER_FOLLOW" text "Ordem: Seguir Todos" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_ORDER_STAY" text "Ordem: Ficar Parado Todos" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_ORDER_DEFEND" text "Ordem: Defender Area Todos" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_DISMISS_ALL" text "Dispensar Todos os NPCs" params_count 0
    0A8E: add_item_to_menu MENU_TITLE_NPC item_key "NPC_BACK" text "Voltar ao Menu Principal" params_count 0

    IF 0A91: is_menu_active MENU_TITLE_NPC
    THEN
        IF 0A92: $selected_npc_menu_item = get_menu_item_selected MENU_TITLE_NPC
        THEN
            // Process Selection
            IF $selected_npc_menu_item == 0 OR $selected_npc_menu_item == 1 // Info items, do nothing
            THEN
                // Do nothing, it's just for display. Menu will redraw.
            ELSIF $selected_npc_menu_item == 2 // Order: Follow
            THEN
                $squad_current_order = 1
                0ACD: show_text_highpriority "NPCs: Seguindo!" time 1500
            ELSIF $selected_npc_menu_item == 3 // Order: Stay
            THEN
                $squad_current_order = 2
                0ACD: show_text_highpriority "NPCs: Parados!" time 1500
            ELSIF $selected_npc_menu_item == 4 // Order: Defend
            THEN
                $squad_current_order = 3
                0ACD: show_text_highpriority "NPCs: Defenderao area!" time 1500
                // Note: npc_recruitment.cs needs to update defend positions when order is applied
            ELSIF $selected_npc_menu_item == 5 // Dismiss All
            THEN
                $trigger_dismiss_all_npcs = true
                0ACD: show_text_highpriority "Sinal para dispensar todos enviado!" time 2000
            ELSIF $selected_npc_menu_item == 6 // Back to Main Menu
            THEN
                0A8F: destroy_menu MENU_TITLE_NPC
                // $main_menu_active_flag should remain true here, as we are going back to main
                0A9C: start_custom_thread_named "MM_SHOW_MAIN_MENU"
                0A93: terminate_this_custom_thread
                BREAK
            END
            WAIT 200 // Debounce selection
        END
    ELSE // Menu is no longer active (e.g., closed with Esc)
        $main_menu_active_flag = false // User closed submenu, so main menu not active either
        0A93: terminate_this_custom_thread
        BREAK
    END
ENDWHILE
0A93: terminate_this_custom_thread // Fallback termination
