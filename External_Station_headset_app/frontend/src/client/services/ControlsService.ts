/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { GameCommand } from '../models/GameCommand';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ControlsService {
    /**
     * Get Game Schema
     * (Tohle zůstává stejné - vrací formuláře pro React)
     * @returns any Successful Response
     * @throws ApiError
     */
    public static getGameSchemaApiGameSchemaGet(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/game/schema',
        });
    }
    /**
     * Send Command To Unity
     * @param sessionId
     * @param deviceId
     * @param requestBody
     * @returns any Successful Response
     * @throws ApiError
     */
    public static sendCommandToUnityApiGameCommandPost(
        sessionId: number,
        deviceId: string,
        requestBody: GameCommand,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/game/command',
            query: {
                'session_id': sessionId,
                'device_id': deviceId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                422: `Validation Error`,
            },
        });
    }
}
