import { TestBed, inject } from '@angular/core/testing';

import { ProductGridService } from './product-grid.service';

describe('ProductGridService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProductGridService]
    });
  });

  it('should be created', inject([ProductGridService], (service: ProductGridService) => {
    expect(service).toBeTruthy();
  }));
});
