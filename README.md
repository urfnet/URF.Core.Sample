# URF.Core.Sample &nbsp;&nbsp; [![Build Status](https://travis-ci.org/urfnet/URF.Core.svg?branch=master)](https://travis-ci.org/urfnet/URF.Core) [![NuGet Badge](https://buildstats.info/nuget/URF.Core.EF.Trackable?includePreReleases=true)](https://www.nuget.org/packages?q=urf.core) #

<sup>Unit-of-Work & Repository Framework | _Official_ [URF](https://github.com/urfnet), [Trackable Entities](https://github.com/TrackableEntities) & Design Factory Team</sup>

[![Build history](https://buildstats.info/travisci/chart/urfnet/URF.Core)](https://travis-ci.org/urfnet/URF.Core/builds)

#### Docs: [comming soon](https://goo.gl/6zh9zp) | Subscribe URF Updates: [@lelong37](http://twitter.com/lelong37) | NuGet: [goo.gl/WEn7Jm](https://goo.gl/WEn7Jm) ####

### Live Demo _(Microsoft Azure)_ ###

* **Northwind.Web** <sup>_(Express, Angular, Node.js, [Kendo UI for Angular](https://www.telerik.com/kendo-angular-ui/components/))</sup>_  
  [http://northwind-web.azurewebsites.net](http://northwind-web.azurewebsites.net/index.html)

* **Northwind.Api** <sup>_(ASP.NET Core Web API, Entity Framework Core, OData, .NET Standard)</sup>_  
  [http://northwind-api.azurewebsites.net/odata/Products?$skip=10&$top=10&$orderby=ProductName desc](http://northwind-api.azurewebsites.net/odata/Products?$skip=10&$top=10&$orderby=ProductName%20desc)

#### Running & Debugging URF.Core.Sample Locally ####
* Clone this repo: https://github.com/urfnet/URF.Core.Sample.git 
* Northwind.Api *<sup>(ASP.NET Core Web API)</sup>*
  * Restore *([northwind.bacpac](https://github.com/urfnet/URF.Core.Sample/tree/master/Northwind.Data/Sql))* Northwind SQL Database
  * Install lastest .NET Core SDK *(URF.Core.Sample is targeting [.NET Core v2.3.1 Preview](https://www.microsoft.com/net/download/dotnet-core/sdk-2.1.300-preview1))*, **after** Visual Studio 2017 installed
  * Open Northwind.sln Solution in Visual Studio 2017
  * Set Northwind.Api as Startup Project for the Solution
  * Verify SQL connection string in [appsettings.json](https://github.com/urfnet/URF.Core.Sample/blob/master/Northwind.Api/appsettings.json#L16) is correct, respective to local environment
  * F5 to launch Northwind.Api in debug mode in Visual Studio
  * Verify http://localhost:2790/odata/Products?$top=10 in Chrome
* Northwind.Web *<sup>(Angular)</sup>*
  * `cd` into Northwind.Web directory
  * run `npm install` *<sup>(install npm dependencies)</sup>*
  * run `ng serve` *<sup>(pre-requisite: [@angular/cli](https://cli.angular.io/))</sup>*
  * Verify http://localhost:4200 in Chrome

#### URF sample and usage in ASP.NET Core Web API & OData *([goo.gl/URdYa1](https://goo.gl/URdYa1))*
Northwind.Api\OData\\**ProductsController.cs**
* Inject IProductsService (Service Pattern)
* Inject IUnitOfWork (UnitOfWork Pattern)
* Using Task, Async, Await as defacto strategy for maximum thread optimization and thread avalibility to handle a maximum number of concurrent HTTP requests without blocking
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
Northwind.Api\\**Startup.cs**
* URF.Core DI & IoC Configuration/Registration Bindings
* JSON Serialization & Deserialization Cyclical Configuration
* ASP.NET Core OData Model Configuration
* ASP.NET Core OData Route Configuraiton
```csharp
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();

        services.AddMvc();

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

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseCors(builder =>
        {
            builder.AllowAnyOrigin();
            builder.AllowAnyHeader();
            builder.AllowAnyMethod();
            builder.AllowCredentials();
            builder.Build();
        });

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
}
```
#### Implementing Domain Logic with URF Service Pattern
* All Methods are Virtual and Overridable as place holders for domain specific implementation business logic. This is the preferred implementation strategy for common developer uses cases e.g. adding any pre or post logic after inserting, updating or deleting.
* Recommended and preferred all Web API Controllers initially injected using Service Pattern from the start. Service Pattern provides a layer for domain logic to reside as the application evolves over time. 
* A natural side effect of the Servie Pattern, is eliminating any potential opportunities for leaky domain implemntations ending up in Controllers. Other than edge cases, the ony concern of a Controller is to serve inbound HTTP requests and dispatch to the right Services.
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

#### Kendo UI Grid Service w/ Asp.Net.Core.OData *(OData v4.x)*
Northwind.Web\src\app\services\\**edit.service.ts**

* Reusable Kendo UI Grid Service, this service's concern is to handle most developer use cases reguarding the Grid e.g. Add, Updating, Deleting, and fetching data, as well as the Grid's state management.
* Change tracking
  * New Items
  * Deleted Items
  * Updated Items
  * Undo, Rollback, Cancel Changes and restore to previous original state
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
  public loading = true;

  constructor (
    private http: HttpClient
    , private resource: string
    , private keys: Array<string>
  ) { super( null ); }

  public read ( queryString = '' ) {

    this.loading = true;

    if (queryString)
      this.queryString = queryString;

    this.fetch()
      .do( data => { this.data = new DataResult( cloneData( data.value ), data.total ); } )
      .do( data => this.originalData = new DataResult( cloneData( data.value ), data.total ) )
      .finally( () => this.loading = false )
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
```

Northwind.Web\src\app\\**app.component.html**
```html
<kendo-grid 
  [kendoGridInCellEditing]="createFormGroup" 
  [editService]="productGridService" 
  [data]="productGridService | async" 
  [pageSize]="productGridService.state.take" 
  [skip]="productGridService.state.skip" 
  [sort]="productGridService.state.sort" 
  [pageable]="true" 
  [sortable]="true" 
  (dataStateChange)="productGridService.onStateChange($event)">
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
Northwind.Web\src\app\services\\**product-grid.service.ts**
* Setup or override default Grid State properties e.g. paging, sorting, filtering, etc.
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

Northwind.Web\src\app\\**app.component.ts**
* Grid implementation & heavy lifting is handled by ProductGridService which extends EditService
* Component/ViewModel is light-weight and clean, due to resuable EditService for any Grid heavy-lifting
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

#### URF.Core Sample Angular & Hosting w/ Node.js in Azure *([goo.gl/QRps9g](https://goo.gl/QRps9g))*

```javascript
var express = require('express')
  , http = require('http')
  , path = require('path');

var app = express();

app.configure(function(){
  app.set('port', process.env.PORT || 3000);
  app.use(express.favicon());
  app.use(express.logger('dev'));
  app.use(express.bodyParser());
  app.use(express.methodOverride());
  app.use(express.static(path.join(__dirname, 'public')));
});

app.configure('development', function(){
  app.use(express.errorHandler());
});

app.get('*', (req, res) => {
  res.sendFile(`index.html`, { root: 'public' });
});

http.createServer(app).listen(app.get('port'), function(){
  console.log("Express server listening on port " + app.get('port'));
});
```
Please see https://github.com/urfnet/URF.Core.Sample/tree/master/Northwind.Web/server, for directory contents and structure for more details for hosting Angular w/ Node.js in Microsoft Azure.

&copy; 2018 [URF.NET](https://github.com/urfnet) All rights reserved.
