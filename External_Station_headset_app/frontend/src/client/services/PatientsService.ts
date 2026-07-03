/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { PatientCreate } from '../models/PatientCreate';
import type { PatientResponse } from '../models/PatientResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class PatientsService {
    /**
     * Create Patient
     * Vytvoří nového pacienta. Nejdřív zkontroluje, jestli kód už neexistuje.
     * @param requestBody
     * @returns PatientResponse Successful Response
     * @throws ApiError
     */
    public static createPatientApiPatientsPatientsPost(
        requestBody: PatientCreate,
    ): CancelablePromise<PatientResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/patients/patients/',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                422: `Validation Error`,
            },
        });
    }
    /**
     * Read Patients
     * Vrátí seznam pacientů. Pokud je vyplněn parametr 'search', funguje jako našeptávač.
     * @param skip
     * @param limit
     * @param search Hledání v kódech pacientů (našeptávač)
     * @returns PatientResponse Successful Response
     * @throws ApiError
     */
    public static readPatientsApiPatientsPatientsGet(
        skip?: number,
        limit: number = 100,
        search?: (string | null),
    ): CancelablePromise<Array<PatientResponse>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/patients/patients/',
            query: {
                'skip': skip,
                'limit': limit,
                'search': search,
            },
            errors: {
                422: `Validation Error`,
            },
        });
    }
}
