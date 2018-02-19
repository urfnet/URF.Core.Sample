import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Rx';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { toODataString, State } from '@progress/kendo-data-query';
import { environment } from '../../environments/environment';
import { GridDataResult } from '@progress/kendo-angular-grid';
import 'rxjs/add/operator/zip';

const cloneData = ( data ) => data.map( item => Object.assign( {}, item ) );

export abstract class EditService extends BehaviorSubject<GridDataResult> {
  private data = new DataResult();
  private originalData = new DataResult();
  private createdItems: any[] = [];
  private updatedItems: any[] = [];
  private deletedItems: any[] = [];
  private errors: any[];
  private state: State;
  private BASE_URL = `${ environment.apiUrl }`;
  private url = `${ this.BASE_URL }${ this.resource }`;
  private queryString: string = '';

  constructor (
    private http: HttpClient
    , private resource: string
    , private keys: Array<string>
  ) { super( null ); }

  public read ( state: State, queryString = '' ) {

    this.state = state;
    this.queryString = queryString;

    if ( this.data.length )
      return super.next( this.data );

    this.fetch()
      .do( data => { this.data = new DataResult( cloneData( data.value ), data.total ); } )
      .do( data => this.originalData = new DataResult( cloneData( data.value ), data.total ) )
      .subscribe( data => { super.next( data ); } );
  }

  public create ( item: any ): void {
    this.createdItems.push( item );
    this.data.unshift( item );
    super.next( this.data );
  }

  public update ( item: any ): void {
    if ( !this.isNew( item ) ) {
      const index = this.itemIndex( item, this.updatedItems );
      if ( index !== -1 )
        this.updatedItems.splice( index, 1, item );
      else
        this.updatedItems.push( item );
    } else {
      const index = this.itemIndex( item, this.createdItems );
      this.createdItems.splice( index, 1, item );
    }
  }

  public remove ( item: any ): void {
    let index = this.itemIndex( item, this.data.value );
    this.data.splice( index, 1 );

    index = this.itemIndex( item, this.createdItems );
    if ( index >= 0 )
      this.createdItems.splice( index, 1 );
    else
      this.deletedItems.push( item );

    index = this.itemIndex( item, this.updatedItems );
    if ( index >= 0 )
      this.updatedItems.splice( index, 1 );

    super.next( this.data );
  }

  public isNew ( item: any ): boolean {
    return this.keys.every( x => !item[ x ] );
  }

  public hasChanges (): boolean {
    return Boolean( this.deletedItems.length || this.updatedItems.length || this.createdItems.length );
  }

  public hasItems (): boolean {
    return Boolean( this.data.length );
  }

  public saveChanges (): void {
    if ( !this.hasChanges() ) return;

    const completed = [];

    this.deletedItems.forEach( item => {
      let uri = `${ this.url }?${ this.keys.map( key => `${ key }=${ item[ key ] }` ).join( '&' ) }`; // e.g. /odata/Orders?CustomerId=3&OrderId=7
      completed.push( this.http.delete( uri ) );
    } );

    this.updatedItems.forEach( item => {
      const uri = `${ this.url }(${ this.keys.map( key => `${ key }=${ item[ key ] }` ).join( ',' ) })`; // e.g. /odata/Orders(CustomerId=3,OrderId=7)
      // const uri = `${ this.url }(${ item[this.keys[0]] })`; // e.g. /odata/Orders(3)
      completed.push( this.http.patch( uri, item ) );
    } );

    this.createdItems.forEach( item => {
      let uri = `${ this.url }`; // e.g. /odata/Orders
      completed.push( this.http.post( uri, item ) );
    } );

    this.reset();

    Observable.zip( ...completed ).subscribe( () => this.read( this.state, this.queryString ) );
  }

  public cancelChanges (): void {
    this.reset();
    this.data = this.originalData;
    this.originalData = new DataResult( cloneData( this.originalData.value ), this.originalData.total );
    super.next( this.data );
  }

  public assignValues ( target: any, source: any ): void {
    Object.assign( target, source );
  }

  private reset () {
    this.data = new DataResult();
    this.deletedItems = [];
    this.updatedItems = [];
    this.createdItems = [];
  }

  private fetch (): Observable<DataResult> {
    const queryStr = `${ toODataString( this.state ) }&$count=true${ this.queryString }`;
    return this.http
      .get( `${ this.url }?${ queryStr }` )
      .map( ( response ) => {
        let data = ( response as any ).value;
        let total = parseInt( response[ '@odata.count' ], 10 );
        return new DataResult( data, total );
      } );
  }

  itemIndex = ( item: any, data: any[] ): number => {
    for ( let idx = 0; idx < data.length; idx++ ) {
      if ( this.keys.every( key => data[ idx ][ key ] === item[ key ] ) ) {
        return idx;
      }
    }
    return -1;
  }

}

// https://en.wikipedia.org/wiki/Adapter_pattern
class DataResult implements GridDataResult {
  data = [];
  total = 0;

  constructor ( data?: any[], total?: number ) {
    this.data = data || [];
    this.total = total || 0;
  }
  unshift = ( item ) => {
    this.data.unshift( item ); this.total++;
  }
  splice = ( index, item ) => {
    this.data.splice( index, item ); this.total--;
  }
  get length () { return this.data.length; }
  map = ( x ) => this.data.map( x );
  get value () { return this.data; }
}
