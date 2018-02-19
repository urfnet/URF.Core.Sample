import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ProductGridService } from './services/product-grid.service';
import { Observable } from 'rxjs/Observable';
import { GridDataResult, DataStateChangeEvent } from '@progress/kendo-angular-grid';
import { State, process,  } from '@progress/kendo-data-query';
import { map } from 'rxjs/operators/map';

@Component( {
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: [ './app.component.scss' ]
} )
export class AppComponent implements OnInit {
  public view: Observable<GridDataResult>;
  public formGroup: FormGroup;
  public gridState: State = {
    sort: [],
    skip: 0,
    take: 10
  };
  public changes: any = {};

  constructor (
    public formBuilder: FormBuilder
    , public productGridService: ProductGridService ) {
    this.createFormGroup = this.createFormGroup.bind( this );

  }

  public ngOnInit (): void {
    this.view = this.productGridService;
    this.productGridService.read(this.gridState, '');
  }

  public onStateChange ( state: DataStateChangeEvent ) {
    this.gridState = state;
    this.productGridService.read(this.gridState, '');
  }

  public createFormGroup ( args: any ): FormGroup {
    const item = args.isNew ? new Product() : args.dataItem;

    this.formGroup = this.formBuilder.group( {
      'ProductId': item.ProductId,
      // 'ProductName': [ args.ProductName, Validators.required ],
      'ProductName': [ item.ProductName ],
      'UnitPrice': item.UnitPrice,
      'UnitsInStock': [ item.UnitsInStock ],
      // 'UnitsInStock': [ args.UnitsInStock, Validators.compose( [ Validators.required, Validators.pattern( '^[0-9]{1,3}' ) ] ) ],
      'Discontinued': item.Discontinued
    } );

    return this.formGroup;
  }
}

export class Product {
  public ProductId: number;
  public ProductName = '';
  public Discontinued = false;
  public UnitsInStock: number;
  public UnitPrice = 0;
}
