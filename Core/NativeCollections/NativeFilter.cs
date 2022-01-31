﻿namespace morpeh.Core.NativeCollections {
    using System;
    using System.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    public struct NativeFilter : IDisposable {
        public NativeArray<NativeArchetype> archetypes;
        [NativeDisableUnsafePtrRestriction] public unsafe int* LengthPtr;
        public unsafe                                     int  Length => *this.LengthPtr;

        public unsafe int this[int index] {
            get {
                var totalArchetypeLength = 0;

                // Find corresponding archetype
                for (int archetypeNum = 0, archetypesCount = this.archetypes.Length; archetypeNum < archetypesCount; archetypeNum++) {
                    var archetype       = this.archetypes[archetypeNum];
                    var archetypeLength = *archetype.lengthPtr;

                    if (index >= totalArchetypeLength && index < totalArchetypeLength + archetypeLength) {
                        //Debug.Log($"{index} Chose archetype: {archetypeNum}");
                        var slotIndex        = index - totalArchetypeLength;
                        var totalSlotsLength = 0;

                        // Find corresponding data in archetype
                        for (int slotNum = 0, slotsLength = *archetype.entitiesBitMap.lengthPtr; slotNum < slotsLength; slotNum++) {
                            var slotLength = archetype.entitiesBitMap.density[slotNum];
                            //Debug.Log($"Density of {slotNum} {slotLength}");
                            if (slotIndex >= totalSlotsLength && slotIndex < totalSlotsLength + slotLength) {
                                
                                // Found corresponding data, searching for exact position
                                var requiredBitJumps = slotIndex - totalSlotsLength;
                                var dataKey          = archetype.entitiesBitMap.data[slotNum];
                                //Debug.Log($"{index} Chose slot: {slotNum} index {slotIndex} with required bit jumps {requiredBitJumps} and value {dataKey}");

                                var positiveShiftsCount = 0;
                                for (int shiftsCount = 0, maxShiftsLength = 31; shiftsCount < maxShiftsLength; shiftsCount++) {
                                    //Debug.Log($"{index} Jumping bit {shiftsCount} {positiveShiftsCount} {dataKey}");
                                    if (positiveShiftsCount == requiredBitJumps) {
                                        var entityId = (slotNum << 5) + shiftsCount;
                                        //Debug.Log($"{index} Entity id found {entityId}");
                                        return entityId;
                                    }
                                    
                                    var bit = (dataKey & (1 << shiftsCount)) != 0;
                                    if (bit) positiveShiftsCount++;
                                }

                                break;
                            }

                            totalSlotsLength += slotLength;
                        }

                        break;
                    }

                    totalArchetypeLength += *archetype.lengthPtr;
                }

                // Could not find entity id
                return -1;
            }
        }

        public void Dispose() {
            this.archetypes.Dispose();
        }
    }
}