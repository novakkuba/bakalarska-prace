/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { SessionCreate } from '../models/SessionCreate';
import type { SessionResponse } from '../models/SessionResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class SessionsService {
    /**
     * Start Session
     * ENDPOINT: Start hry.
     * Vezme ID pacienta a ID headsetu a vrátí nové session_id.
     * @param requestBody
     * @returns SessionResponse Successful Response
     * @throws ApiError
     */
    public static startSessionApiSessionsPost(
        requestBody: SessionCreate,
    ): CancelablePromise<SessionResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/sessions/',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                422: `Validation Error`,
            },
        });
    }
    /**
     * Stop Session
     * ENDPOINT: Konec hry.
     * Uzavře sezení v databázi.
     * @param sessionId
     * @returns SessionResponse Successful Response
     * @throws ApiError
     */
    public static stopSessionApiSessionsSessionIdEndPost(
        sessionId: number,
    ): CancelablePromise<SessionResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/sessions/{session_id}/end',
            path: {
                'session_id': sessionId,
            },
            errors: {
                422: `Validation Error`,
            },
        });
    }
}
