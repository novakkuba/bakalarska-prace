/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { LogResponse } from '../models/LogResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class LogsService {
    /**
     * Read Logs
     * API Endpoint: GET /api/logs
     * Returns a list of the most recent events.
     * @param limit
     * @returns LogResponse Successful Response
     * @throws ApiError
     */
    public static readLogsApiLogsGet(
        limit: number = 100,
    ): CancelablePromise<Array<LogResponse>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/logs/',
            query: {
                'limit': limit,
            },
            errors: {
                422: `Validation Error`,
            },
        });
    }
}
