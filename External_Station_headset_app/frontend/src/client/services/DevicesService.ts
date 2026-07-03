/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DeviceResponse } from '../models/DeviceResponse';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class DevicesService {
    /**
     * Get Online Devices
     * Vrátí všechny připojené brýle.
     * React si to zavolá pro naplnění roletky při výběru headsetu.
     * @returns DeviceResponse Successful Response
     * @throws ApiError
     */
    public static getOnlineDevicesApiDevicesDevicesOnlineGet(): CancelablePromise<Array<DeviceResponse>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/devices/devices/online',
        });
    }
    /**
     * Get All Devices
     * DEBUG: Vrátí úplně všechna zařízení v databázi, bez ohledu na to, zda jsou online.
     * @returns any Successful Response
     * @throws ApiError
     */
    public static getAllDevicesApiDevicesDevicesGet(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/devices/devices/',
        });
    }
}
