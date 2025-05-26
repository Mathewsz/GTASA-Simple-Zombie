// CLEO Script: Base Building System
// Author: Your Name
// Version: 1.0

{$CLEO .cs}

//----------------------------------------------------------------------------------
// --- GLOBAL VARIABLES & CONSTANTS ---
//----------------------------------------------------------------------------------
CONST
    MAX_BARRICADES: int = 50
    BARRICADE_MODEL_ID_CONST: int = 360 // Model #barricade2
    FILENAME_BASES: string = "bases.dat" // Save file
    PLACEMENT_DISTANCE: float = 3.0
END

VAR
    $placed_barricades_coords[MAX_BARRICADES][3]: float // X, Y, Z
    $placed_barricades_handles[MAX_BARRICADES]: OBJECT_HANDLE // Store object handles
    $placed_barricades_model_ids[MAX_BARRICADES]: int   // Store model ID for each
    $active_placed_barricades_count: int = 0
    $bb_script_initialized: bool = false // To ensure load runs once
END

// External Global Variables
VAR_EXTERNAL
    $player_barricade_kits: int // From crafting.cs
    $trigger_save_bases: bool   // From main_menu.cs
END

//----------------------------------------------------------------------------------
// --- LOAD BARRICADES THREAD (Runs Once) ---
//----------------------------------------------------------------------------------
03A4: name_thread "BB_LOAD_DATA"

IF NOT $bb_script_initialized // Ensure this runs only once
THEN
    0A98: $FILE_EXISTS = does_file_exist FILENAME_BASES
    IF $FILE_EXISTS == 1
    THEN
        0A9A: open_file FILENAME_BASES mode "rb" store_to $FILE_HANDLE
        IF $FILE_HANDLE > 0 // Check if file opened successfully
        THEN
            0A9E: $active_placed_barricades_count = read_file $FILE_HANDLE size 4
            
            // Clamp count to prevent buffer overflow if file is corrupted/edited
            IF $active_placed_barricades_count > MAX_BARRICADES
            THEN
                $active_placed_barricades_count = MAX_BARRICADES
            END
            IF $active_placed_barricades_count < 0 // Should not happen with unsigned read, but good practice
            THEN
                $active_placed_barricades_count = 0
            END

            FOR $I = 0 TO $active_placed_barricades_count - 1
                VAR $temp_x, $temp_y, $temp_z: float
                VAR $temp_model_id: int
                
                0A9E: $temp_x = read_file $FILE_HANDLE size 4
                0A9E: $temp_y = read_file $FILE_HANDLE size 4
                0A9E: $temp_z = read_file $FILE_HANDLE size 4
                0A9E: $temp_model_id = read_file $FILE_HANDLE size 4

                // Re-create the object
                0107: $NEW_BARRICADE_HANDLE = create_object $temp_model_id at $temp_x $temp_y $temp_z
                
                // Store loaded data
                $placed_barricades_coords[$I][0] = $temp_x
                $placed_barricades_coords[$I][1] = $temp_y
                $placed_barricades_coords[$I][2] = $temp_z
                $placed_barricades_handles[$I] = $NEW_BARRICADE_HANDLE
                $placed_barricades_model_ids[$I] = $temp_model_id
            ENDFOR
            0A9B: close_file $FILE_HANDLE
            0ACD: show_text_highpriority "Bases carregadas!" time 2000
        ELSE
            0ACD: show_text_highpriority "ERRO: Nao foi possivel abrir bases.dat para leitura" time 3000
        END
    END
    $bb_script_initialized = true
END
0A93: terminate_this_custom_thread // Load thread runs once and terminates

//----------------------------------------------------------------------------------
// --- BARRICADE PLACEMENT THREAD ---
//----------------------------------------------------------------------------------
03A4: name_thread "BB_PLACEMENT"

WHILE true
    WAIT 0 // Check every frame for key press

    IF 00D6: if 0AB0: key_just_pressed 0x45 // 'E' key (VK_KEY_E)
    THEN
        IF $player_barricade_kits > 0
        THEN
            IF $active_placed_barricades_count < MAX_BARRICADES
            THEN
                // Calculate spawn position
                VAR $player_x, $player_y, $player_z, $player_heading: float
                00AA: store_actor $PLAYER_ACTOR position_to $player_x $player_y $player_z
                00E0: $player_heading = player $PLAYER_CHAR heading

                VAR $spawn_x, $spawn_y, $spawn_z: float
                // Using Sanny Builder's math functions (sin/cos expect degrees)
                $spawn_x = $player_x + (PLACEMENT_DISTANCE * sin($player_heading))
                $spawn_y = $player_y + (PLACEMENT_DISTANCE * cos($player_heading))
                $spawn_z = $player_z // Initial Z, will adjust to ground

                VAR $ground_z: float
                03CB: $ground_z = get_ground_z_for_3d_coord $spawn_x $spawn_y $spawn_z if_not_found -100.0
                
                IF $ground_z > -99.0 // Check if ground was found (not -100.0)
                THEN
                    $player_barricade_kits -= 1 // Consume kit

                    // Create barricade object
                    0107: $NEW_BARRICADE_OBJECT_HANDLE = create_object BARRICADE_MODEL_ID_CONST at $spawn_x $spawn_y $ground_z
                    
                    // Store barricade data
                    $placed_barricades_coords[$active_placed_barricades_count][0] = $spawn_x
                    $placed_barricades_coords[$active_placed_barricades_count][1] = $spawn_y
                    $placed_barricades_coords[$active_placed_barricades_count][2] = $ground_z
                    $placed_barricades_handles[$active_placed_barricades_count] = $NEW_BARRICADE_OBJECT_HANDLE
                    $placed_barricades_model_ids[$active_placed_barricades_count] = BARRICADE_MODEL_ID_CONST
                    
                    $active_placed_barricades_count += 1

                    0ACD: show_text_highpriority "Barricada colocada!" time 2000
                    // 03CF: play_audio_at_player $PLAYER_CHAR audio_id 1084 // Optional: Placement sound

                    // --- SAVE BARRICADES LOGIC (called after successful placement) ---
                    0A9A: open_file FILENAME_BASES mode "wb" store_to $SAVE_FILE_HANDLE
                    IF $SAVE_FILE_HANDLE > 0
                    THEN
                        0A9F: write_file $SAVE_FILE_HANDLE value $active_placed_barricades_count size 4 // Save total count
                        FOR $J = 0 TO $active_placed_barricades_count - 1
                            0A9F: write_file $SAVE_FILE_HANDLE value $placed_barricades_coords[$J][0] size 4 // X
                            0A9F: write_file $SAVE_FILE_HANDLE value $placed_barricades_coords[$J][1] size 4 // Y
                            0A9F: write_file $SAVE_FILE_HANDLE value $placed_barricades_coords[$J][2] size 4 // Z
                            0A9F: write_file $SAVE_FILE_HANDLE value $placed_barricades_model_ids[$J] size 4 // Model ID
                        ENDFOR
                        0A9B: close_file $SAVE_FILE_HANDLE
                        0ACD: show_text_highpriority "Bases salvas!" time 1000
                    ELSE
                        0ACD: show_text_highpriority "ERRO: Nao foi possivel salvar bases.dat" time 3000
                    END
                    // --- END OF SAVE BARRICADES LOGIC ---
                ELSE
                    0ACD: show_text_highpriority "Nao e possivel colocar a barricada aqui." time 2000
                    // Kit is not consumed if placement fails due to ground not found
                END
            ELSE
                0ACD: show_text_highpriority "Limite maximo de barricadas atingido." time 2000
            END
        ELSE
            0ACD: show_text_highpriority "Nao tens kits de barricada!" time 2000
        END
        WAIT 500 // Debounce 'E' key after processing
    END
END

//----------------------------------------------------------------------------------
// --- CHECK SAVE TRIGGER THREAD (from main_menu.cs) ---
//----------------------------------------------------------------------------------
03A4: name_thread "BB_SAVE_TRIGGER"

WHILE true
    WAIT 500 // Check periodically

    IF $trigger_save_bases // Check if the flag is set by main_menu.cs
    THEN
        IF $active_placed_barricades_count > 0 // Only save if there's something to save
        THEN
            // Replicate essential save logic here
            0A9A: open_file FILENAME_BASES mode "wb" store_to $SAVE_FILE_HANDLE_TRIGGER
            IF $SAVE_FILE_HANDLE_TRIGGER > 0
            THEN
                0A9F: write_file $SAVE_FILE_HANDLE_TRIGGER value $active_placed_barricades_count size 4
                FOR $K = 0 TO $active_placed_barricades_count - 1
                    0A9F: write_file $SAVE_FILE_HANDLE_TRIGGER value $placed_barricades_coords[$K][0] size 4 // X
                    0A9F: write_file $SAVE_FILE_HANDLE_TRIGGER value $placed_barricades_coords[$K][1] size 4 // Y
                    0A9F: write_file $SAVE_FILE_HANDLE_TRIGGER value $placed_barricades_coords[$K][2] size 4 // Z
                    0A9F: write_file $SAVE_FILE_HANDLE_TRIGGER value $placed_barricades_model_ids[$K] size 4 // Model ID
                ENDFOR
                0A9B: close_file $SAVE_FILE_HANDLE_TRIGGER
                0ACD: show_text_highpriority "Bases salvas via menu!" time 2000
            ELSE
                0ACD: show_text_highpriority "ERRO: Nao foi possivel salvar bases.dat (trigger)" time 3000
            END
        ELSE
            0ACD: show_text_highpriority "Nenhuma base para salvar." time 2000
        END
        $trigger_save_bases = false // Reset the flag
    END
END
