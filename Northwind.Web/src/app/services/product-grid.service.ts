import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { EditService } from './edit.service';

@Injectable()
export class ProductGridService extends EditService {

  constructor (http: HttpClient) {
    super( http, 'Products', [ 'ProductId' ] );
    this.state = {
      sort: [],
      skip: 0,
      take: 10
    };
  }
}
