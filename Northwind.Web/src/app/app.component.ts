import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ProductGridService } from './services/product-grid.service';
import { Observable } from 'rxjs/Observable';
import { GridDataResult, DataStateChangeEvent } from '@progress/kendo-angular-grid';
import { State, process,  } from '@progress/kendo-data-query';
import { map } from 'rxjs/operators/map';
import { Product } from './models/product';

@Component( {
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: [ './app.component.scss' ]
} )
export class AppComponent implements OnInit {
  public formGroup: FormGroup;
  public changes: any = {};

  constructor (
    public formBuilder: FormBuilder
    , public productGridService: ProductGridService ) {
    this.createFormGroup = this.createFormGroup.bind( this );
  }

  public ngOnInit (): void {
    this.productGridService.read();
  }

  public createFormGroup ( args: any ): FormGroup {
    const item = args.isNew ? new Product() : args.dataItem;

    this.formGroup = this.formBuilder.group( {
      'ProductId': item.ProductId,
      'ProductName': [ item.ProductName, Validators.required ],
      'UnitPrice': item.UnitPrice,
      'UnitsInStock': [ item.UnitsInStock, Validators.required ],
      'Discontinued': item.Discontinued
    } );

    return this.formGroup;
  }
}
