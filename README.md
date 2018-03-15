# URF.Core.Sample [![Build Status](https://travis-ci.org/urfnet/URF.Core.svg?branch=master)](https://travis-ci.org/urfnet/URF.Core) #
**_<sup>URF.Core Sample Application | Unit-of-Work & Repository Framework | Official URF Team & [Trackable Entities](https://github.com/TrackableEntities) Team</sup>_**
#### Docs: [comming soon](https://goo.gl/6zh9zp) | Subscribe URF Updates: [@lelong37](http://twitter.com/lelong37) | NuGet: [goo.gl/WEn7Jm](https://goo.gl/WEn7Jm) ####
#### Live Demo _(Microsoft Azure)_ ####

* Northwind.Web _(Express, Angular, Node.js, [Kendo UI for Angular](https://www.telerik.com/kendo-angular-ui/components/))_  
  **url**: [http://northwind-web.azurewebsites.net](http://northwind-web.azurewebsites.net/index.html)

* Northwind.Api _(ASP.NET Core Web API, Entity Framework Core, OData, .NET Standard)_  
  **url**: [http://northwind-api.azurewebsites.net/odata/Products?$skip=10&$top=10&$orderby=ProductName desc](http://northwind-api.azurewebsites.net/odata/Products?$skip=10&$top=10&$orderby=ProductName%20desc)

#### URF sample and usage in ASP.NET Core Web API & OData *([goo.gl/URdYa1](https://goo.gl/URdYa1))*
```csharp
public class ProductsController : ODataController
{
    private readonly IProductService _productService;
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(
        IProductService productService,
        IUnitOfWork unitOfWork)
    {
        _productService = productService;
        _unitOfWork = unitOfWork;
    }

    // e.g. GET odata/Products?$skip=2&$top=10
    [EnableQuery]
    public IQueryable<Products> Get() => _productService.Queryable();

    // e.g.  GET odata/Products(37)
    public async Task<IActionResult> Get([FromODataUri] int key)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await _productService.FindAsync(key);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // e.g. PUT odata/Products(37)
    public async Task<IActionResult> Put([FromODataUri] int key, [FromBody] Products products)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (key != products.ProductId)
            return BadRequest();

        _productService.Update(products);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _productService.ExistsAsync(key))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // e.g. PUT odata/Products
    public async Task<IActionResult> Post([FromBody] Products products)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _productService.Insert(products);
        await _unitOfWork.SaveChangesAsync();

        return Created(products);
    }

    // e.g. PATCH, MERGE odata/Products(37)
    [AcceptVerbs("PATCH", "MERGE")]
    public async Task<IActionResult> Patch([FromODataUri] int key, [FromBody] Delta<Products> product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = await _productService.FindAsync(key);
        if (entity == null)
            return NotFound();

        product.Patch(entity);
        _productService.Update(entity);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _productService.ExistsAsync(key))
                return NotFound();
            throw;
        }
        return Updated(entity);
    }

    // e.g. DELETE odata/Products(37)
    public async Task<IActionResult> Delete([FromODataUri] int key)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _productService.DeleteAsync(key);

        if (!result)
            return NotFound();

        await _unitOfWork.SaveChangesAsync();

        return StatusCode((int) HttpStatusCode.NoContent);
    }
}
```
#### URF sample and usage in ASP.NET Core Web API & OData (*https://goo.gl/ZF6JAH*)
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc()
        .AddJsonOptions(options =>
            options.SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All);

    services.AddOData();

    var connectionString = Configuration.GetConnectionString(nameof(NorthwindContext));
    services.AddDbContext<NorthwindContext>(options => options.UseSqlServer(connectionString));
    services.AddScoped<DbContext, NorthwindContext>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<ITrackableRepository<Products>, TrackableRepository<Products>>();
    services.AddScoped<IProductService, ProductService>();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
        app.UseDeveloperExceptionPage();

    var oDataConventionModelBuilder = new ODataConventionModelBuilder(app.ApplicationServices);
    var entitySetConfiguration = oDataConventionModelBuilder.EntitySet<Products>(nameof(Products));        
    entitySetConfiguration.EntityType.HasKey(x => x.ProductId);
    entitySetConfiguration.EntityType.Ignore(x => x.Category);
    entitySetConfiguration.EntityType.Ignore(x => x.Supplier);
    entitySetConfiguration.EntityType.Ignore(x => x.OrderDetails);

    app.UseMvc(routeBuilder =>
        {
            routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
            routeBuilder.MapODataServiceRoute("ODataRoute", "odata", oDataConventionModelBuilder.GetEdmModel()); 
            routeBuilder.EnableDependencyInjection();
        }
    );
}
```
#### Implementing Domain Logic with URF Service Pattern (*https://goo.gl/n3Bbgc*)
```csharp
public class CustomerService : Service<Customer>, ICustomerService
{
    private readonly ITrackableRepository<Order> _ordeRepository;

    public CustomerService(
        ITrackableRepository<Customer> customerRepository,
        ITrackableRepository<Order> ordeRepository) : base(customerRepository)
    {
        _ordeRepository = ordeRepository;
    }

    public async Task<IEnumerable<Customer>> CustomersByCompany(string companyName)
    {
        return await Repository
            .Queryable()
            .Where(x => x.CompanyName.Contains(companyName))
            .ToListAsync();
    }

    public async Task<decimal> CustomerOrderTotalByYear(string customerId, int year)
    {
        return await Repository
            .Queryable()
            .Where(c => c.CustomerId == customerId)
            .SelectMany(c => c.Orders.Where(o => o.OrderDate != null && o.OrderDate.Value.Year == year))
            .SelectMany(c => c.OrderDetails)
            .Select(c => c.Quantity * c.UnitPrice)
            .SumAsync();
    }

    public async Task<IEnumerable<CustomerOrder>> GetCustomerOrder(string country)
    {
        var customers = Repository.Queryable();
        var orders = _ordeRepository.Queryable();

        var query = from c in customers
            join o in orders on new { a = c.CustomerId, b = c.Country }
                equals new { a = o.CustomerId, b = country }
            select new CustomerOrder
            {
                CustomerId = c.CustomerId,
                ContactName = c.ContactName,
                OrderId = o.OrderId,
                OrderDate = o.OrderDate
            };

        return await query.ToListAsync();
    }
}
```

#### Kendo UI Grid Service w/ Asp.Net.Core.OData

```typescript
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/Rx';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { toODataString, State } from '@progress/kendo-data-query';
import { environment } from '../../environments/environment';
import { GridDataResult, DataStateChangeEvent } from '@progress/kendo-angular-grid';
import 'rxjs/add/operator/zip';

const cloneData = ( data ) => data.map( item => Object.assign( {}, item ) );

export abstract class EditService extends BehaviorSubject<GridDataResult> {
  private data = new DataResult();
  private originalData = new DataResult();
  private createdItems: any[] = [];
  private updatedItems: any[] = [];
  private deletedItems: any[] = [];
  private errors: any[];
  public state: State;
  private baseUrl = `${ environment.apiUrl }`;
  private url = `${ this.baseUrl }${ this.resource }`;
  private queryString = '';

  constructor (
    private http: HttpClient
    , private resource: string
    , private keys: Array<string>
  ) { super( null ); }

  public read ( queryString = '' ) {
    if (queryString)
      this.queryString = queryString;

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
      let uri = `${ this.url }(${ item[ this.keys[ 0 ] ] })`; // e.g. /odata/Orders(3)

      if ( this.keys.length > 1 )
        uri = `${ this.url }(${ this.keys.map( key => `${ item[ key ] }` ).join( '&' ) })`; // e.g. /odata/Orders(CustomerId=3,OrderId=7)

      completed.push( this.http.delete( uri ) );
    } );

    this.updatedItems.forEach( item => {
      let uri = `${ this.url }(${ this.keys.map( key => `${ item[ key ] }` ).join( '&' ) })`; // e.g. /odata/Orders(3)

      if ( this.keys.length > 1 )
        uri = `${ this.url }(${ this.keys.map( key => `${ key }=${ item[ key ] }` ).join( ',' ) })`; // e.g. /odata/Orders(CustomerId=3,OrderId=7)

      completed.push( this.http.patch( uri, item ) );
    } );

    this.createdItems.forEach( item => {
      const uri = `${ this.url }`; // e.g. /odata/Orders
      completed.push( this.http.post( uri, item ) );
    } );

    this.reset();

    Observable.zip( ...completed ).subscribe( () => this.read( this.queryString ) );
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

  public onStateChange ( state: DataStateChangeEvent ) {
    this.state = state;
    this.read(this.queryString);
  }

  private fetch (): Observable<DataResult> {
    const queryStr = `${ toODataString( this.state ) }&$count=true${ this.queryString }`;
    return this.http
      .get( `${ this.url }?${ queryStr }` )
      .map( ( response ) => {
        const data = ( response as any ).value;
        const total = parseInt( response[ '@odata.count' ], 10 );
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
```

#### app.component.html ####
```html
<kendo-grid [kendoGridInCellEditing]="createFormGroup" [editService]="productGridService" [data]="productGridService | async" [pageSize]="productGridService.state.take" [skip]="productGridService.state.skip" [sort]="productGridService.state.sort" [pageable]="true" [sortable]="true" (dataStateChange)="productGridService.onStateChange($event)">
  <ng-template kendoGridToolbarTemplate>
    <button kendoGridAddCommand>Add new</button>
    <button class='k-button' [disabled]="!productGridService.hasChanges()" (click)="productGridService.saveChanges();">Save Changes</button>
    <button class='k-button' [disabled]="!productGridService.hasChanges()" (click)="productGridService.cancelChanges();">Cancel Changes</button>
  </ng-template>
  <kendo-grid-column field="ProductId" title="Id" [editable]="false"></kendo-grid-column>
  <kendo-grid-column field="ProductName" title="Product Name"></kendo-grid-column>
  <kendo-grid-column field="UnitPrice" editor="numeric" title="Price"></kendo-grid-column>
  <kendo-grid-column field="Discontinued" editor="boolean" title="Discontinued"></kendo-grid-column>
  <kendo-grid-column field="UnitsInStock" editor="numeric" title="Units In Stock"></kendo-grid-column>
  <kendo-grid-command-column title="" width="220">
    <ng-template kendoGridCellTemplate let-isNew="isNew">
      <button kendoGridRemoveCommand>Remove</button>
      <button kendoGridSaveCommand [disabled]="formGroup?.invalid">Add</button>
      <button kendoGridCancelCommand>Discard</button>
    </ng-template>
  </kendo-grid-command-column>
</kendo-grid>
```
#### product-grid.service.ts ####
```typescript
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
```

#### app.component.ts ####
```typescript
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
```