// CLEO Script: NPC Recruitment System
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
CONST
    MAX_RECRUITED_NPCS: int = 3
    RECRUIT_KEY: int = 0x52 // 'R'
    DISMISS_KEY: int = 0x58 // 'X'
    ORDER_CYCLE_KEY: int = 0x47 // 'G'
    NPC_DEFAULT_WEAPON: int = 24 // Desert Eagle
    NPC_DEFAULT_AMMO: int = 200
    DEFEND_RADIUS: float = 15.0
END

VAR
    $recruited_npcs_handles[MAX_RECRUITED_NPCS]: ACTOR_HANDLE
    $recruited_npcs_status[MAX_RECRUITED_NPCS]: int // 0=empty, 1=following, 2=staying, 3=defending
    $recruited_npcs_defend_pos_x[MAX_RECRUITED_NPCS]: float
    $recruited_npcs_defend_pos_y[MAX_RECRUITED_NPCS]: float
    $recruited_npcs_defend_pos_z[MAX_RECRUITED_NPCS]: float
    $recruited_npcs_count: int = 0
    $squad_current_order: int = 1 // Default order: 1 (Following)
    $npc_script_initialized: bool = false
END

// External Global Variables
VAR_EXTERNAL
    $trigger_dismiss_all_npcs: bool // From main_menu.cs
END

//----------------------------------------------------------------------------------
// --- INITIALIZATION (Runs once at script start) ---
//----------------------------------------------------------------------------------
IF NOT $npc_script_initialized
THEN
    FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
        $recruited_npcs_handles[$i] = 0
        $recruited_npcs_status[$i] = 0
    ENDFOR
    $npc_script_initialized = true
END

//----------------------------------------------------------------------------------
// --- KEY CHECK THREAD (Recruit, Dismiss, Order Cycle) ---
//----------------------------------------------------------------------------------
03A4: name_thread "NPC_KEY_CHECK"

WHILE true
    WAIT 0 // Check keys every frame

    // --- RECRUIT NPC (R Key) ---
    IF 00D6: if 0AB0: key_just_pressed RECRUIT_KEY
    THEN
        IF $recruited_npcs_count < MAX_RECRUITED_NPCS
        THEN
            05D0: $TARGET_ACTOR = get_actor_player_is_aiming_at
            IF $TARGET_ACTOR > 0 AND $TARGET_ACTOR <> $PLAYER_ACTOR // Valid actor and not player
            THEN
                IF 0118: not is_actor_dead $TARGET_ACTOR
                THEN
                    VAR $already_recruited: bool = false
                    FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
                        IF $recruited_npcs_handles[$i] == $TARGET_ACTOR
                        THEN
                            $already_recruited = true
                            BREAK
                        END
                    ENDFOR

                    IF NOT $already_recruited
                    THEN
                        VAR $is_police: bool = false
                        VAR $is_gang: bool = false
                        020A: $is_police = is_actor $TARGET_ACTOR model_police
                        020B: $is_gang = is_actor $TARGET_ACTOR model_gangmember

                        IF NOT $is_police AND NOT $is_gang
                        THEN
                            VAR $slot_found: bool = false
                            VAR $slot_index: int = -1
                            FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
                                IF $recruited_npcs_handles[$i] == 0
                                THEN
                                    $slot_index = $i
                                    $slot_found = true
                                    BREAK
                                END
                            ENDFOR

                            IF $slot_found
                            THEN
                                $recruited_npcs_handles[$slot_index] = $TARGET_ACTOR
                                $recruited_npcs_count += 1
                                $recruited_npcs_status[$slot_index] = $squad_current_order // Assign current squad order

                                0571: set_actor $TARGET_ACTOR relationship_with_actor $PLAYER_CHAR to RESPECT
                                0445: set_actor $TARGET_ACTOR immune_to_player_bullets 1
                                01B2: give_actor $TARGET_ACTOR weapon NPC_DEFAULT_WEAPON ammo NPC_DEFAULT_AMMO
                                0215: set_actor $TARGET_ACTOR money 0
                                060B: set_actor $TARGET_ACTOR decision_maker_to 32 // Aggressive
                                01AF: set_actor $TARGET_ACTOR stay_in_same_vehicle_as_player 1 // Try to stay in car with player

                                IF $squad_current_order == 3 // If current order is Defend Area
                                THEN
                                    00AA: store_actor $PLAYER_ACTOR position_to $recruited_npcs_defend_pos_x[$slot_index] $recruited_npcs_defend_pos_y[$slot_index] $recruited_npcs_defend_pos_z[$slot_index]
                                END
                                0ACD: show_text_highpriority "Sobrevivente recrutado!" time 2000
                            END
                        ELSE
                            0ACD: show_text_highpriority "Nao pode recrutar policiais ou membros de gangue." time 2000
                        END
                    ELSE
                        0ACD: show_text_highpriority "Este sobrevivente ja foi recrutado." time 2000
                    END
                END
            END
        ELSE
            0ACD: show_text_highpriority "Limite maximo de recrutas atingido." time 2000
        END
        WAIT 300 // Debounce
    END

    // --- DISMISS NPC (X Key) ---
    IF 00D6: if 0AB0: key_just_pressed DISMISS_KEY
    THEN
        05D0: $TARGET_ACTOR_TO_DISMISS = get_actor_player_is_aiming_at
        IF $TARGET_ACTOR_TO_DISMISS > 0
        THEN
            VAR $dismiss_slot_index: int = -1
            FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
                IF $recruited_npcs_handles[$i] == $TARGET_ACTOR_TO_DISMISS
                THEN
                    $dismiss_slot_index = $i
                    BREAK
                END
            ENDFOR

            IF $dismiss_slot_index > -1
            THEN
                0571: set_actor $TARGET_ACTOR_TO_DISMISS relationship_with_actor $PLAYER_CHAR to NEUTRAL
                0445: set_actor $TARGET_ACTOR_TO_DISMISS immune_to_player_bullets 0
                01C8: remove_actor $TARGET_ACTOR_TO_DISMISS weapon NPC_DEFAULT_WEAPON
                05D1: clear_actor_task $TARGET_ACTOR_TO_DISMISS immediate 1
                01AF: set_actor $TARGET_ACTOR_TO_DISMISS stay_in_same_vehicle_as_player 0

                $recruited_npcs_handles[$dismiss_slot_index] = 0
                $recruited_npcs_status[$dismiss_slot_index] = 0
                $recruited_npcs_count -= 1
                0ACD: show_text_highpriority "Sobrevivente dispensado." time 2000
            ELSE
                0ACD: show_text_highpriority "Mire em um sobrevivente recrutado para dispensar." time 2000
            END
        END
        WAIT 300 // Debounce
    END

    // --- CYCLE ORDERS (G Key) ---
    IF 00D6: if 0AB0: key_just_pressed ORDER_CYCLE_KEY
    THEN
        IF $squad_current_order == 1 // Was Following, now Staying
        THEN
            $squad_current_order = 2
            0ACD: show_text_highpriority "Equipe: Parado!" time 2000
        ELSIF $squad_current_order == 2 // Was Staying, now Defending
        THEN
            $squad_current_order = 3
            VAR $temp_player_x, $temp_player_y, $temp_player_z: float
            00AA: store_actor $PLAYER_ACTOR position_to $temp_player_x $temp_player_y $temp_player_z
            FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
                IF $recruited_npcs_handles[$i] > 0
                THEN
                    $recruited_npcs_defend_pos_x[$i] = $temp_player_x
                    $recruited_npcs_defend_pos_y[$i] = $temp_player_y
                    $recruited_npcs_defend_pos_z[$i] = $temp_player_z
                END
            ENDFOR
            0ACD: show_text_highpriority "Equipe: Defender Area!" time 2000
        ELSIF $squad_current_order == 3 // Was Defending, now Following
        THEN
            $squad_current_order = 1
            0ACD: show_text_highpriority "Equipe: Seguindo!" time 2000
        END

        // Apply new order to all active NPCs
        FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
            IF $recruited_npcs_handles[$i] > 0
            THEN
                $recruited_npcs_status[$i] = $squad_current_order
            END
        ENDFOR
        WAIT 300 // Debounce
    END
END // End of Key Check Loop

//----------------------------------------------------------------------------------
// --- NPC BEHAVIOR MANAGEMENT THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "NPC_BEHAVIOR"

WHILE true
    WAIT 250 // Check and update NPC behavior periodically

    FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
        VAR $NPC_HANDLE: ACTOR_HANDLE
        $NPC_HANDLE = $recruited_npcs_handles[$i]

        IF $NPC_HANDLE > 0 // If slot is active
        THEN
            // --- Death/Invalid Handle Check ---
            IF 0118: is_actor_dead $NPC_HANDLE
            OR 80A2: not actor $NPC_HANDLE exists // 80A2 is 'actor handle is valid AND actor exists'
            THEN
                // Clear slot if NPC is dead or handle became invalid
                0ACD: show_text_highpriority "Um sobrevivente morreu." time 2000 // Optional message
                $recruited_npcs_handles[$i] = 0
                $recruited_npcs_status[$i] = 0
                $recruited_npcs_count -= 1
                CONTINUE // Skip to next NPC in loop
            END

            // --- Apply Task Based on Status ---
            VAR $current_status: int = $recruited_npcs_status[$i]
            VAR $current_task_id: int
            029A: $current_task_id = get_actor $NPC_HANDLE task_status // Get primary task ID

            IF $current_status == 1 // Following
            THEN
                // TASK_FOLLOW_PLAYER_FOOTSTEPS = 53
                IF $current_task_id <> 53 // Only assign if not already doing it
                THEN
                    05D1: clear_actor_task $NPC_HANDLE immediate 0 // Clear secondary tasks, let primary finish
                    0520: set_actor $NPC_HANDLE objective TASK_FOLLOW_PLAYER_FOOTSTEPS $PLAYER_ACTOR
                END
            ELSIF $current_status == 2 // Staying
            THEN
                // TASK_STAND_STILL = 2
                IF $current_task_id <> 2
                THEN
                    05D1: clear_actor_task $NPC_HANDLE immediate 1
                    04C9: task_stand_still $NPC_HANDLE time -1
                END
            ELSIF $current_status == 3 // Defending Area
            THEN
                // TASK_GUARD_AREA = 52
                 IF $current_task_id <> 52 // This check might be too simple for TASK_GUARD_AREA
                 THEN
                    05D1: clear_actor_task $NPC_HANDLE immediate 0
                    0521: set_actor $NPC_HANDLE objective TASK_GUARD_AREA $recruited_npcs_defend_pos_x[$i] $recruited_npcs_defend_pos_y[$i] $recruited_npcs_defend_pos_z[$i] radius DEFEND_RADIUS min_time 5000 max_time 10000
                 END
            END
        END // IF $NPC_HANDLE > 0
    ENDFOR // FOR loop through NPCs
END // End of Behavior Management Loop

//----------------------------------------------------------------------------------
// --- CHECK DISMISS ALL NPCS TRIGGER THREAD (from main_menu.cs) ---
//----------------------------------------------------------------------------------
03A4: name_thread "NPC_DISMISS_ALL_TRIGGER"

WHILE true
    WAIT 500 // Check periodically

    IF $trigger_dismiss_all_npcs
    THEN
        VAR $dismissed_count_this_cycle: int = 0
        FOR $i = 0 TO MAX_RECRUITED_NPCS - 1
            VAR $NPC_TO_DISMISS_HANDLE: ACTOR_HANDLE
            $NPC_TO_DISMISS_HANDLE = $recruited_npcs_handles[$i]

            IF $NPC_TO_DISMISS_HANDLE > 0 // If slot is active
            THEN
                // Ensure actor exists and is alive before trying to modify
                IF 00A2: actor $NPC_TO_DISMISS_HANDLE exists
                AND 0118: not is_actor_dead $NPC_TO_DISMISS_HANDLE
                THEN
                    0571: set_actor $NPC_TO_DISMISS_HANDLE relationship_with_actor $PLAYER_CHAR to NEUTRAL
                    0445: set_actor $NPC_TO_DISMISS_HANDLE immune_to_player_bullets 0
                    01C8: remove_actor $NPC_TO_DISMISS_HANDLE weapon NPC_DEFAULT_WEAPON
                    05D1: clear_actor_task $NPC_TO_DISMISS_HANDLE immediate 1
                    01AF: set_actor $NPC_TO_DISMISS_HANDLE stay_in_same_vehicle_as_player 0
                    $dismissed_count_this_cycle += 1
                END
                // Clear slot regardless of actor state, if it was marked as recruited
                $recruited_npcs_handles[$i] = 0
                $recruited_npcs_status[$i] = 0
            END
        ENDFOR

        IF $dismissed_count_this_cycle > 0
        THEN
            0ACD: show_text_highpriority "Todos os NPCs dispensados via menu!" time 2000
        ELSE
            IF $recruited_npcs_count == 0 // If no one was dismissed because count was already 0
            THEN
                 0ACD: show_text_highpriority "Nenhum NPC para dispensar." time 2000
            END
        END
        
        $recruited_npcs_count = 0 // Reset count fully
        $trigger_dismiss_all_npcs = false // Reset the flag
    END
END
