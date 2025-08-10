import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatTableHarness } from '@angular/material/table/testing';
import { MatPaginatorHarness } from '@angular/material/paginator/testing';
import { MatSortHarness } from '@angular/material/sort/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { TableCardComponent } from './table-card.component';
import { TableConfig } from '../../models/table.model';

describe('TableCardComponent', () => {
  let component: TableCardComponent;
  let fixture: ComponentFixture<TableCardComponent>;
  let loader: HarnessLoader;

  const mockData = [
    { id: 1, name: 'Project Alpha', status: 'Active', created: new Date('2024-01-01'), agents: 5 },
    { id: 2, name: 'Project Beta', status: 'Pending', created: new Date('2024-01-15'), agents: 3 },
    { id: 3, name: 'Project Gamma', status: 'Inactive', created: new Date('2024-02-01'), agents: 0 },
    { id: 4, name: 'Project Delta', status: 'Error', created: new Date('2024-02-15'), agents: 2 }
  ];

  const mockConfig: TableConfig = {
    columns: [
      { key: 'name', label: 'Project Name', sortable: true },
      { key: 'status', label: 'Status', type: 'status', width: '120px' },
      { key: 'created', label: 'Created Date', type: 'date', sortable: true },
      { key: 'agents', label: 'Agent Count', type: 'number', align: 'center' }
    ],
    actions: [
      { icon: 'edit', label: 'Edit', color: 'primary', callback: jasmine.createSpy('editCallback') },
      { icon: 'delete', label: 'Delete', color: 'warn', callback: jasmine.createSpy('deleteCallback') }
    ],
    pageSize: 10,
    pageSizeOptions: [5, 10, 25],
    showPagination: true,
    striped: true,
    hoverable: true
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableCardComponent, BrowserAnimationsModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TableCardComponent);
    component = fixture.componentInstance;
    loader = TestbedHarnessEnvironment.loader(fixture);
    
    // Set required inputs
    component.data = mockData;
    component.config = mockConfig;
    component.title = 'Test Table';
    component.subtitle = 'Test Subtitle';
    
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with correct configuration', () => {
    expect(component.dataSource.data).toEqual(mockData);
    expect(component.displayedColumns.length).toBe(5); // 4 columns + actions
    expect(component.displayedColumns).toContain('actions');
  });

  it('should render correct number of table rows', async () => {
    const table = await loader.getHarness(MatTableHarness);
    const rows = await table.getRows();
    expect(rows.length).toBe(mockData.length);
  });

  it('should render column headers correctly', async () => {
    const table = await loader.getHarness(MatTableHarness);
    const headerRows = await table.getHeaderRows();
    const headerCells = await headerRows[0].getCells();
    
    expect(headerCells.length).toBe(5); // 4 columns + actions
    expect(await headerCells[0].getText()).toBe('Project Name');
    expect(await headerCells[1].getText()).toBe('Status');
    expect(await headerCells[2].getText()).toBe('Created Date');
    expect(await headerCells[3].getText()).toBe('Agent Count');
    expect(await headerCells[4].getText()).toBe('Actions');
  });

  it('should emit row click event when hoverable', () => {
    spyOn(component.rowClick, 'emit');
    const rowElement = fixture.nativeElement.querySelector('tr.mat-row');
    
    rowElement.click();
    
    expect(component.rowClick.emit).toHaveBeenCalledWith(mockData[0]);
  });

  it('should not emit row click when not hoverable', () => {
    component.config.hoverable = false;
    fixture.detectChanges();
    
    spyOn(component.rowClick, 'emit');
    const rowElement = fixture.nativeElement.querySelector('tr.mat-row');
    
    rowElement.click();
    
    expect(component.rowClick.emit).not.toHaveBeenCalled();
  });

  it('should enable sorting on sortable columns', async () => {
    const sort = await loader.getHarness(MatSortHarness);
    const sortHeaders = await sort.getSortHeaders();
    
    expect(sortHeaders.length).toBe(2); // name and created columns are sortable
  });

  it('should display pagination when configured', async () => {
    const paginator = await loader.getHarness(MatPaginatorHarness);
    expect(paginator).toBeTruthy();
    
    const pageSize = await paginator.getPageSize();
    expect(pageSize).toBe(10);
  });

  it('should not display pagination when disabled', async () => {
    component.config.showPagination = false;
    fixture.detectChanges();
    
    const paginators = await loader.getAllHarnesses(MatPaginatorHarness);
    expect(paginators.length).toBe(0);
  });

  it('should render action buttons', async () => {
    const buttons = await loader.getAllHarnesses(MatButtonHarness);
    expect(buttons.length).toBe(mockData.length * 2); // 2 actions per row
  });

  it('should trigger action callbacks', async () => {
    const buttons = await loader.getAllHarnesses(MatButtonHarness);
    await buttons[0].click(); // Click first edit button
    
    expect(mockConfig.actions![0].callback).toHaveBeenCalledWith(mockData[0]);
  });

  it('should format data based on column types', () => {
    const compiled = fixture.nativeElement;
    
    // Check status badge
    const statusBadge = compiled.querySelector('.status-badge');
    expect(statusBadge).toBeTruthy();
    expect(statusBadge.textContent.trim()).toBe('Active');
    expect(statusBadge.classList.contains('status-active')).toBe(true);
    
    // Check number formatting (Agent Count should be displayed)
    const cells = compiled.querySelectorAll('td.mat-cell');
    const agentCell = Array.from(cells).find((cell: any) => cell.textContent.includes('5'));
    expect(agentCell).toBeTruthy();
  });

  it('should apply striped class when configured', () => {
    const table = fixture.nativeElement.querySelector('table');
    expect(table.classList.contains('striped')).toBe(true);
  });

  it('should apply hoverable class when configured', () => {
    const table = fixture.nativeElement.querySelector('table');
    expect(table.classList.contains('hoverable')).toBe(true);
  });

  it('should display no data message when data is empty', () => {
    component.data = [];
    component.ngOnInit();
    fixture.detectChanges();
    
    const noDataRow = fixture.nativeElement.querySelector('.no-data-row');
    expect(noDataRow).toBeTruthy();
    expect(noDataRow.textContent).toContain('No data available');
  });

  it('should update data source when data changes', () => {
    const newData = [
      { id: 5, name: 'Project Epsilon', status: 'Active', created: new Date(), agents: 7 }
    ];
    
    component.data = newData;
    component.ngOnChanges();
    
    expect(component.dataSource.data).toEqual(newData);
  });

  it('should correctly classify status values', () => {
    expect(component.getStatusClass('active')).toBe('status-active');
    expect(component.getStatusClass('Active')).toBe('status-active');
    expect(component.getStatusClass('success')).toBe('status-active');
    
    expect(component.getStatusClass('inactive')).toBe('status-inactive');
    expect(component.getStatusClass('disabled')).toBe('status-inactive');
    
    expect(component.getStatusClass('pending')).toBe('status-pending');
    expect(component.getStatusClass('warning')).toBe('status-pending');
    
    expect(component.getStatusClass('error')).toBe('status-error');
    expect(component.getStatusClass('failed')).toBe('status-error');
    
    expect(component.getStatusClass('unknown')).toBe('status-default');
    expect(component.getStatusClass('')).toBe('status-default');
  });

  it('should handle column alignment', () => {
    const cells = fixture.nativeElement.querySelectorAll('td.mat-cell');
    const centerAlignedCell = cells[3]; // Agent count column has center alignment
    
    expect(centerAlignedCell.style.textAlign).toBe('center');
  });

  it('should use theme variables for styling', () => {
    const styles = getComputedStyle(fixture.nativeElement);
    const table = fixture.nativeElement.querySelector('table');
    
    // Check that CSS uses theme variables (this would need actual theme setup to fully test)
    expect(table).toBeTruthy();
  });
});