// CLEO Script: Zombie Spawn
// Author: Your Name
// Version: 1.1 - Handles Array and Correct Counting

{$CLEO .cs}

// Main thread name
03A4: name_thread "ZOMBIE_SPAWN_MAIN"

// Global variables
VAR
    $zombie_mode_active: bool = false // Tracks if zombie mode is active
    $active_zombies: int = 0 // Counts active zombies
    $max_zombies: int = 20 // Maximum number of zombies
    $ZOMBIE_MODEL: int = #CS_BUSINESSMAN // Model ID for zombies (148)
    $ZOMBIE_WEAPON: int = 0 // Weapon ID for zombies (0 = fists)
    $ZOMBIE_HEALTH: int = 50 // Health for zombies
    // Sound Attraction Variables
    $sound_event_active: bool = false
    $sound_event_pos_x: float = 0.0
    $sound_event_pos_y: float = 0.0
    $sound_event_pos_z: float = 0.0
    $sound_event_start_time: int = 0 // Timestamp when sound event started
END
VAR
    $zombie_handles[$max_zombies]: ACTOR_HANDLE // Array to store zombie actor handles
END

// Constants
CONST
    SOUND_EVENT_DURATION: int = 10000 // 10 seconds in milliseconds
    SOUND_ATTRACTION_RADIUS: float = 40.0 // Zombies within this radius will be attracted
END

// Initialize zombie handles array
FOR $i = 0 TO $max_zombies - 1
    $zombie_handles[$i] = 0 // Initialize with 0 or an invalid handle marker
ENDFOR

// Main loop
WHILE true
    WAIT 0 // Process game logic every frame

    // Check for 'Y' key press to toggle zombie mode
    IF 00DF:   actor $PLAYER_ACTOR driving
    THEN
        // Player is driving, don't check for key press (avoids conflicts)
    ELSE
        // Check for 'Y' key press to toggle zombie mode
        IF 0AB0: is_key_pressed 0 0x59 // Key 'Y' (VK_KEY_Y)
        THEN
            WAIT 0 // ensure key press is registered before toggling
            $zombie_mode_active = NOT $zombie_mode_active

            // Display message based on mode state
            IF $zombie_mode_active
            THEN
                0ACD: show_text_highpriority "Zombie mode activated!" time 2000
            ELSE
                0ACD: show_text_highpriority "Zombie mode deactivated!" time 2000
                // Deactivate mode: remove all active zombies
                FOR $i = 0 TO $max_zombies - 1
                    IF $zombie_handles[$i] <> 0 // Check if handle is valid
                    THEN
                        // Ensure actor exists before deleting
                        IF 00A2: actor $zombie_handles[$i] // returns true if actor exists
                        THEN
                           00A7: delete_actor $zombie_handles[$i]
                        END
                        $zombie_handles[$i] = 0 // Clear handle from array
                    END
                ENDFOR
                $active_zombies = 0
            END
            WAIT 500 // Debounce key press
        END
    END

    // Zombie logic loop (runs only when mode is active)
    IF $zombie_mode_active
    THEN
        // --- Sound Event Detection ---
        IF NOT $sound_event_active // Only detect new sounds if no event is currently active
        THEN
            // 1. Player Shooting
            IF 02A3: is_player $PLAYER_ID shooting // Check if player is shooting
            THEN
                00AA: store_actor $PLAYER_ACTOR position_to $sound_event_pos_x $sound_event_pos_y $sound_event_pos_z
                $sound_event_active = true
                0005: $sound_event_start_time = game_timer_in_ms
                0ACD: show_text_highpriority "DEBUG: Player Shot Sound" time 1000 // Debug
            ELSE
                // 2. Explosions
                VAR $explosion_check_radius: float = 50.0
                VAR $player_cur_x, $player_cur_y, $player_cur_z: float
                00AA: store_actor $PLAYER_ACTOR position_to $player_cur_x $player_cur_y $player_cur_z
                VAR $x1, $y1, $z1, $x2, $y2, $z2: float
                $x1 = $player_cur_x - $explosion_check_radius
                $y1 = $player_cur_y - $explosion_check_radius
                $z1 = $player_cur_z - 20.0 // Check a bit lower/higher for Z
                $x2 = $player_cur_x + $explosion_check_radius
                $y2 = $player_cur_y + $explosion_check_radius
                $z2 = $player_cur_z + 20.0
                IF 030D: is_there_an_explosion_in_area $x1 $y1 $z1 $x2 $y2 $z2
                THEN
                    030E: store_last_explosion_coordinates_to $sound_event_pos_x $sound_event_pos_y $sound_event_pos_z
                    $sound_event_active = true
                    0005: $sound_event_start_time = game_timer_in_ms
                    0ACD: show_text_highpriority "DEBUG: Explosion Sound" time 1000 // Debug
                ELSE
                    // 3. Player Horn
                    IF 01F6: is_player $PLAYER_ID in_any_car
                    THEN
                        IF 03EC: is_player $PLAYER_ID pressing_horn
                        THEN
                            VAR $player_vehicle: CAR_HANDLE
                            01F9: $player_vehicle = Nth_car_player $PLAYER_ID is_in 0 // Get car player is in
                            IF 00A2: does_vehicle_exist $player_vehicle // Check if vehicle handle is valid
                            THEN
                                00A4: store_car $player_vehicle position_to $sound_event_pos_x $sound_event_pos_y $sound_event_pos_z
                                $sound_event_active = true
                                0005: $sound_event_start_time = game_timer_in_ms
                                0ACD: show_text_highpriority "DEBUG: Horn Sound" time 1000 // Debug
                            END
                        END
                    END
                END
            END
        ELSE // $sound_event_active is true, manage its timer
            VAR $current_time_ms: int
            0005: $current_time_ms = game_timer_in_ms
            IF $current_time_ms - $sound_event_start_time > SOUND_EVENT_DURATION
            THEN
                $sound_event_active = false
                0ACD: show_text_highpriority "DEBUG: Sound Expired" time 1000 // Debug
            END
        END

        // --- Zombie Cleanup & AI Update Loop ---
        VAR $temp_dead_zombies_this_cycle: int = 0
        FOR $idx = 0 TO $max_zombies - 1
            VAR $current_zombie_handle: ACTOR_HANDLE
            $current_zombie_handle = $zombie_handles[$idx]

            IF $current_zombie_handle <> 0 // If handle is valid (points to an actor)
            THEN
                IF 00A2: not actor $current_zombie_handle // Check if actor handle is no longer valid in game (despawned, etc.)
                THEN
                    $zombie_handles[$idx] = 0 // Clear from our array
                    $temp_dead_zombies_this_cycle = $temp_dead_zombies_this_cycle + 1 // Count as if it's dead for our logic
                ELSIF 0118: is_actor_dead $current_zombie_handle // Check if actor exists in game AND is dead
                THEN
                    // Actor is confirmed dead
                    IF 00A2: actor $current_zombie_handle // Check if it still somehow exists before deleting
                    THEN
                        00A7: delete_actor $current_zombie_handle // Remove from game
                    END
                    $zombie_handles[$idx] = 0 // Clear from our array
                    $temp_dead_zombies_this_cycle = $temp_dead_zombies_this_cycle + 1
                ELSE
                    // --- Enhanced Zombie AI Logic (including sound attraction) ---
                    VAR $zombie_is_investigating_sound: bool = false

                    IF $sound_event_active
                    THEN
                        00AA: store_actor $current_zombie_handle position_to ZOMBIE_X ZOMBIE_Y ZOMBIE_Z // Get current zombie pos
                        VAR $distance_to_sound: float
                        00AB: $distance_to_sound = distance_between_XYZ ZOMBIE_X ZOMBIE_Y ZOMBIE_Z and_XYZ $sound_event_pos_x $sound_event_pos_y $sound_event_pos_z
                        
                        IF $distance_to_sound < SOUND_ATTRACTION_RADIUS
                        THEN
                            // Check if zombie is already heading to this sound's location (approx)
                            // This is a simple check; a more robust way would be to store task target coords or use a flag
                            VAR $task_target_x, $task_target_y, $task_target_z: float
                            // There isn't a direct opcode to get task target coords.
                            // For now, we will re-task. If it's already going there, it's a minor inefficiency.
                            
                            05D1: clear_actor_task $current_zombie_handle immediate 1 // Clear primary tasks to prioritize sound
                            // 05D8: task_go_to_coord $current_zombie_handle x $sound_event_pos_x y $sound_event_pos_y z $sound_event_pos_z mode 1 time SOUND_EVENT_DURATION // Mode 1 = run
                            // Using 04BF for more control over speed and stopping radius
                            04BF: task_go_to_coord_any_means $current_zombie_handle x $sound_event_pos_x y $sound_event_pos_y z $sound_event_pos_z speed 2.0 model -1 radius 2.0 // Speed 2.0 = run
                            $zombie_is_investigating_sound = true // Zombie is now busy investigating sound
                        END
                    END

                    // If zombie is not investigating a sound, or if sound investigation is overridden by immediate threats:
                    IF NOT $zombie_is_investigating_sound 
                    THEN
                        VAR $player_actor_handle: ACTOR_HANDLE = $PLAYER_ACTOR
                        VAR $player_visible: bool = false
                        02A0: can_actor_see_actor $current_zombie_handle $player_actor_handle store_to $player_visible
                        
                        00AA: store_actor $player_actor_handle position_to PLAYER_X PLAYER_Y PLAYER_Z
                        00AA: store_actor $current_zombie_handle position_to ZOMBIE_X ZOMBIE_Y ZOMBIE_Z // Re-fetch zombie pos if not already fetched
                        VAR $distance_to_player: float
                        00AB: $distance_to_player = distance_between_XYZ PLAYER_X PLAYER_Y PLAYER_Z and_XYZ ZOMBIE_X ZOMBIE_Y ZOMBIE_Z
                        
                        VAR $has_target: bool = false

                        // Priority 1: Player
                        IF $player_visible AND $distance_to_player < 20.0 
                        THEN
                            05D1: clear_actor_task $current_zombie_handle immediate 1 // Clear other tasks if player is close
                            0521: set_actor $current_zombie_handle objective TASK_KILL_PLAYER $player_actor_handle
                            $has_target = true
                        ELSE
                            // Priority 2: Nearby NPCs
                            VAR $nearest_npc: ACTOR_HANDLE = 0
                            01F0: get_nearest_actor_to_actor $current_zombie_handle radius 15.0 find_next 0 store_to $nearest_npc
                            
                            IF $nearest_npc <> 0 AND $nearest_npc <> $player_actor_handle
                            THEN
                                IF 0118: not is_actor_dead $nearest_npc
                                THEN
                                    VAR $is_target_another_zombie: bool = false
                                    FOR $j = 0 TO $max_zombies - 1
                                        IF $zombie_handles[$j] <> 0 AND $zombie_handles[$j] == $nearest_npc
                                        THEN
                                            $is_target_another_zombie = true
                                            BREAK
                                        END
                                    ENDFOR

                                    IF NOT $is_target_another_zombie
                                    THEN
                                        05D1: clear_actor_task $current_zombie_handle immediate 1 // Clear other tasks
                                        0521: set_actor $current_zombie_handle objective TASK_KILL_PED $nearest_npc
                                        $has_target = true
                                    END
                                END
                            END
                        END

                        // Priority 3: Wander if no target found
                        IF NOT $has_target
                        THEN
                            VAR $current_task_primary_id: int
                            029A: get_actor $current_zombie_handle task_status store_to $current_task_primary_id
                            IF $current_task_primary_id <> 4 AND $current_task_primary_id <> 5
                            THEN
                               04C8: task_wander_standard $current_zombie_handle
                            END
                        END
                    END // END IF NOT $zombie_is_investigating_sound
                END // End of AI logic for living zombie
            END // End of check for valid handle
        ENDFOR // End of loop through zombie_handles
        // Decrement count by number of dead/invalid found
        // Ensure $active_zombies doesn't go below zero if $temp_dead_zombies_this_cycle is unexpectedly large
        IF $temp_dead_zombies_this_cycle > $active_zombies
        THEN
            $active_zombies = 0
        ELSE
            $active_zombies = $active_zombies - $temp_dead_zombies_this_cycle
        END


        // --- Zombie Spawning ---
        IF $active_zombies < $max_zombies
        THEN
            // Find an empty slot in the handles array for the new zombie
            VAR $empty_slot_found: bool = false
            VAR $spawn_slot: int = -1
            FOR $i = 0 TO $max_zombies - 1
                IF $zombie_handles[$i] == 0
                THEN
                    $spawn_slot = $i
                    $empty_slot_found = true
                    BREAK // Found an empty slot
                END
            ENDFOR

            IF $empty_slot_found // Proceed to spawn if an empty slot was found
            THEN
                // Spawn a new zombie
                00A5: create_actor PEDTYPE_CIVMALE (4) model $ZOMBIE_MODEL at 0.0 0.0 0.0 // Placeholder coordinates
                VAR $NEW_ZOMBIE: ACTOR_HANDLE
                $NEW_ZOMBIE = LAST_CREATED_ACTOR
                
                IF 00A2: actor $NEW_ZOMBIE // Check if actor was created successfully
                THEN
                    $zombie_handles[$spawn_slot] = $NEW_ZOMBIE // Store handle

                    // Store player coordinates
                    00AA: store_actor $PLAYER_ACTOR position_to PLAYER_X PLAYER_Y PLAYER_Z
                
                    // Calculate random spawn coordinates (10-20 meters away from player)
                    VAR $spawn_dist: float
                    008B: $spawn_dist = random_float_in_ranges 10.0 20.0
                    VAR $rand_angle_deg: float
                    008B: $rand_angle_deg = random_float_in_ranges 0.0 360.0
                    VAR $rand_angle_rad: float
                    $rand_angle_rad = $rand_angle_deg * 0.0174532925 // Degrees to radians

                    VAR $offset_x: float = $spawn_dist * COS($rand_angle_rad)
                    VAR $offset_y: float = $spawn_dist * SIN($rand_angle_rad)
                    
                    VAR $final_spawn_x: float = PLAYER_X + $offset_x
                    VAR $final_spawn_y: float = PLAYER_Y + $offset_y
                    VAR $final_spawn_z: float
                    03CB: get_ground_z_for_3d_coord $final_spawn_x $final_spawn_y PLAYER_Z store_to $final_spawn_z
                    IF 0030: $final_spawn_z == 0.0 // If ground_z not found, use player's Z + offset
                    THEN
                        $final_spawn_z = PLAYER_Z + 1.0
                    END

                    00A6: $NEW_ZOMBIE position $final_spawn_x $final_spawn_y $final_spawn_z

                    // Set zombie properties
                    0530: set_actor $NEW_ZOMBIE health $ZOMBIE_HEALTH
                    01B2: give_actor $NEW_ZOMBIE weapon $ZOMBIE_WEAPON ammo 1 // Give fists
                    01C9: set_actor $NEW_ZOMBIE walk_style #PEDMOVE_DRUNK // Set zombie-like walk style. Others: #PEDMOVE_FAT, #PEDMOVE_SHUFFLE. Full list in ped.ifp.

                    // Initial task: attack player. This will be refined by AI logic almost immediately.
                    0521: set_actor $NEW_ZOMBIE objective TASK_KILL_PLAYER $PLAYER_ACTOR
                    
                    $active_zombies = $active_zombies + 1

                    // Wait before spawning another zombie
                    WAIT 5000 // 5 seconds 
                END // if $NEW_ZOMBIE exists
            END // if $empty_slot_found
        END // if $active_zombies < $max_zombies
    END // if $zombie_mode_active

END // End of main loop
