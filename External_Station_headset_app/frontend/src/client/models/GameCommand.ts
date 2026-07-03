/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AttentionTrackingSettings } from './AttentionTrackingSettings';
import type { CorsiBlocksSettings } from './CorsiBlocksSettings';
import type { LocationRecallSettings } from './LocationRecallSettings';
import type { MrPuzzleSettings } from './MrPuzzleSettings';
import type { RotationCubeSettings } from './RotationCubeSettings';
export type GameCommand = {
    payload: (RotationCubeSettings | CorsiBlocksSettings | LocationRecallSettings | AttentionTrackingSettings | MrPuzzleSettings);
};

