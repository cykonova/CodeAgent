# MockupML Language Specification v1.0-alpha

## Overview

MockupML is a declarative markup language designed for rapid UI mockup generation. It uses a bracket-based syntax similar to CSS and JavaScript, making it intuitive for designers and developers to create interactive prototypes.

## ✨ Key Features

- 🎨 **23+ UI Elements**: Complete set of layout, content, data, and navigation elements
- 🖼️ **Rich Media Support**: Base64 images, FontAwesome & Material Icons (120+ icons)
- 📱 **Multi-Screen Apps**: Interactive navigation with state management
- 🧮 **Mathematical Properties**: Responsive scaling with `*=`, `+=`, `-=`, `/=` operators
- 📊 **Semantic HTML**: Proper tables, forms, navigation with accessibility support
- 🌐 **Navigation Tools**: Breadcrumbs, multi-level menus, screen transitions
- 🎯 **Tree Structures**: Hierarchical data display with expand/collapse functionality
- 🔤 **Text Preservation**: Maintains all punctuation and formatting
- 🎨 **Theme Isolation**: Preview themes are isolated from editor light/dark mode
- 📋 **Data Tables**: Striped, bordered, with row/column spanning
- 🎪 **Live Preview**: Real-time rendering with syntax highlighting
- 🎨 **Material Design**: Professional UI styling with light/dark themes
- 💬 **C-Style Comments**: Line and block comments for documentation
- ⚠️ **Enhanced Error Handling**: Precise line/column error reporting
- 📐 **Flexible Layouts**: Row, column, and grid layouts with alignment

## Breaking Changes & Deprecations

### v1.0-alpha

- **Toolbar `theme` property deprecated**: Use `style` instead
  - Old: `toolbar(theme=primary)`
  - New: `toolbar(style=primary)`

## Language Philosophy

- **Declarative**: Describe what you want, not how to build it
- **Visual**: Syntax mirrors the visual hierarchy of UI components
- **Mathematical**: Support for computed properties and responsive scaling
- **Interactive**: Multi-screen navigation with state management
- **Comprehensive**: Full icon libraries, base64 images, semantic HTML
- **Minimalist**: Clean syntax with sensible defaults
- **Modular**: Stream-based parser with expandable interpreter architecture

## Architecture

MockupML features a completely refactored modular parser architecture:

- **StreamReader**: Character-by-character processing with position tracking
- **Tokenizer**: Stream-based lexical analysis with error recovery
- **InterpreterRegistry**: Pluggable interpreter system for element types
- **Specialized Interpreters**: Individual parsers for each element category

## Syntax Grammar

### Basic Structure

```
element(property=value, property=value) {
    text { content }
    child_element(properties) {
        text { nested_content }
    }
}
```

### Comments

MockupML supports C-style comments for documentation and notes:

```
// Single-line comment
screen(device=desktop) {
    /* Multi-line comment
       can span multiple lines */
    header {
        text { Welcome } // End-of-line comment
    }
}
```

**Comment Types:**
- `// text` - Single-line comment (rest of line ignored)
- `/* text */` - Block comment (can span multiple lines)
- Comments can appear anywhere in the code and are completely ignored during parsing

### Text Content

Text content is provided using the `text` element with brace syntax. MockupML preserves all punctuation and formatting within text blocks.

**Features:**
- **Punctuation preservation**: Maintains commas, periods, apostrophes, symbols
- **Formatting support**: Handles complex text like prices, emails, times
- **Style variants**: Muted text styling for subtle content
- **Mathematical sizing**: Font size scaling with mathematical operators

```
text { Hello, world! How are you? }
text { Price: $12,345.00 }
text { Email: user@example.com }
text { Time: 12:30 PM }
text(fontSize*=1.5) { Large Text }
text(style=muted) { Subtle text with styling }
```

### Mathematical Properties

Properties support mathematical operators for responsive scaling:

```
text(fontSize*=1.5) { Large Text }    // Multiply by 1.5
text(fontSize+=4) { Bigger Text }     // Add 4 pixels
text(fontSize-=2) { Smaller Text }    // Subtract 2 pixels
text(fontSize/=2) { Half Size }       // Divide by 2
```

## Element Reference

MockupML elements are organized into two categories: **Block Elements** (containers) and **Non-Block Elements** (content).

### Block Elements

Block elements are container elements that can contain other elements.

#### `screen`
Root container element that defines the viewport and base properties. Supports different screen types for various presentation modes.

**Properties:**
- `device`: Enum (`desktop`, `tablet`, `mobile`) - Target device type (required for standard screens)
- `fontSize`: Number - Base font size for mathematical scaling (defaults to 12 for standard screens)
- `id`: String - Screen identifier for navigation
- `initial`: Boolean - Mark as initial screen in multi-screen apps
- `type`: Enum (`standard`, `modal`, `sidecar`) - Screen presentation type (defaults to `standard`)
- `title`: String - Screen title (required for modal and sidecar types)
- `width`: Number - Screen width in pixels (required for sidecar, optional for modal)
- `side`: Enum (`left`, `right`) - Side for sidecar screens (defaults to `right`)

**Screen Types:**

**Standard Screen** - Full-page screen with device-specific styling:
```
screen(device=desktop, fontSize=14, id="home", initial=true) {
    // Standard screen content
}
```

**Modal Screen** - Popup overlay with centered content:
```
screen(type="modal", id="confirm", title="Confirm Action", width=500) {
    text { Are you sure you want to proceed? }
    toolbar(style=transparent) {
        expander
        button(style=secondary, label="Cancel", data-action="close")
        button(style=primary, label="Confirm")
    }
}
```

**Sidecar Screen** - Sliding side panel with configurable width and position:
```
screen(type="sidecar", id="settings", title="Settings", width=400, side="right") {
    text { Settings panel content }
    field(label="Theme", type=dropdown, options="Light,Dark,Auto")
    button(style=primary, label="Save")
}
```

**Features:**
- **Multiple screen types**: Standard, modal popup, and sliding sidecar panels
- **Smooth transitions**: Animated show/hide effects for each type
- **Close functionality**: Click overlay, close button, or press ESC to close modals/sidecars
- **Responsive design**: Adapts to different screen sizes
- **Accessibility**: Proper ARIA attributes and keyboard navigation

#### `header`
Page or section header container.

```
header {
    text { Welcome to MockupML }
    button(style=primary, label="Get Started")
}
```

#### `nav`
Navigation container for menus and navigation elements.

```
nav {
    text { Home }
    text { About }
    text { Contact }
}
```

#### `text`
Text display block element.

```
text { This is a text block }
text { Multi-line text content can go here }
```

#### `content`
Main content area container.

```
content {
    text { Main content goes here }
    button(label="Action Button")
}
```

#### `sidebar`
Side panel container with optional width control.

**Properties:**
- `width`: Number - Width in pixels

```
sidebar(width=300) {
    text { Navigation Menu }
}
```

#### `panel`
Generic container panel for grouping elements. (`card` is an alias for `panel`)

**Properties:**
- `style`: Enum - Visual style variant

```
panel {
    text { Panel content }
    button(label="Panel Action")
}

// card is an alias for panel
card {
    text { Card content }
}
```

#### `table`
Table container for structured data display with semantic HTML output.

**Properties:**
- `style`: Enum (`striped`, `bordered`, `hover`) - Table styling options (can combine multiple)

**Features:**
- **Semantic HTML**: Generates proper `<table>`, `<tr>`, `<td>` elements
- **Style variants**: Striped rows, borders, hover effects
- **Responsive design**: Adapts to different screen sizes
- **Header support**: Special styling for header rows

```
table(style="striped bordered") {
    row(type=header) {
        cell { text { Name } }
        cell { text { Email } }
        cell { text { Status } }
    }
    row {
        cell { text { John Doe } }
        cell { text { john@example.com } }
        cell { text { Active } }
    }
}
```

##### `row`
Table row element. Only allowed within `table` elements.

**Properties:**
- `type`: Enum (`header`, `footer`) - Special row types
- `rowSpan`: Number - Number of rows to span

```
row(type=header, rowSpan=2) {
    cell { text { Header Cell } }
}
```

##### `cell`
Table cell element. Only allowed within `row` elements.

**Properties:**
- `colSpan`: Number - Number of columns to span
- `rowSpan`: Number - Number of rows to span

```
cell(colSpan=2) { text { Spanning Cell } }
cell { button(label="Action") }
```

#### `layout`
Flexible layout container with different layout types.

**Properties:**
- `type`: Enum (`grid`, `row`, `column`) - Layout type
- `cols`: Number - Number of columns (for grid type)
- `align`: Enum (`left`, `right`, `top`, `bottom`, `center`) - Flow alignment (for row/column types)

```
// Grid layout with columns
layout(type=grid, cols=2) {
    panel { text { Item 1 } }
    panel { text { Item 2 } }
    panel { text { Item 3 } }
    panel { text { Item 4 } }
}

// Row layout with alignment
layout(type=row, align=center) {
    button(style=primary, label="Save")
    button(style=secondary, label="Cancel")
}

// Column layout
layout(type=column, align=left) {
    text { Header }
    text { Subheader }
    text { Content }
}
```

#### `tree`
Hierarchical tree structure container for displaying nested data.

**Properties:**
- `expanded`: Boolean - Whether tree starts expanded (defaults to true)

**Features:**
- **Hierarchical display**: Shows parent-child relationships with indentation
- **Expandable nodes**: Supports collapsible tree sections
- **Icon indicators**: Automatically adds folder/file icons
- **Nested structure**: Supports unlimited nesting levels

```
tree {
    node(label="Root Folder", expanded=true) {
        node(label="Documents") {
            node(label="file1.txt")
            node(label="file2.pdf")
        }
        node(label="Images") {
            node(label="photo1.jpg")
            node(label="photo2.png")
        }
    }
}
```

#### `node`
Tree node element representing an item in a hierarchical structure.

**Properties:**
- `label`: String - Node display text
- `expanded`: Boolean - Whether node children are visible (defaults to true)
- `type`: Enum (`folder`, `file`) - Node type for appropriate styling

**Features:**
- **Expandable content**: Can contain child nodes
- **Visual indicators**: Shows expand/collapse icons
- **Semantic structure**: Proper nesting and indentation

```
node(label="Parent Node", expanded=false) {
    node(label="Child 1", type="file")
    node(label="Child 2", type="folder") {
        node(label="Grandchild", type="file")
    }
}
```

#### `toolbar`
Application toolbar container for actions, menus, and navigation. Supports theming to match different UI contexts.

**Properties:**
- `style`: Enum (`primary`, `secondary`, `light`, `dark`, `transparent`) - Visual style (replaces deprecated `theme` property)
- `position`: Enum (`top`, `bottom`, `sticky`, `fixed`) - Toolbar positioning

**Features:**
- **Theme variants**: Multiple color schemes to match different contexts
- **Flexible content**: Supports buttons, icon buttons, dropdowns, and custom content
- **Sticky positioning**: Can stick to top/bottom of viewport
- **Semantic HTML**: Uses `<nav>` element for accessibility

```
// Primary themed toolbar with mixed content
toolbar(style=primary, position=sticky) {
    button(icon="menu", label="")  // Icon-only button
    text { My App }
    dropdown(label="File", icon="folder") {
        menuitem(label="New", icon="file-plus", shortcut="Ctrl+N")
        menuitem(label="Open", icon="folder-open", shortcut="Ctrl+O")
        menuitem(type=divider)
        menuitem(label="Recent Files", type=header)
        menuitem(label="project.mml", transitionTo="editor")
    }
    dropdown(label="Edit") {
        menuitem(label="Cut", icon="cut", shortcut="Ctrl+X")
        menuitem(label="Copy", icon="copy", shortcut="Ctrl+C")
        menuitem(label="Paste", icon="paste", shortcut="Ctrl+V")
    }
}

// Transparent toolbar for overlays
toolbar(style=transparent) {
    button(style=primary, label="Save")
    button(style=secondary, label="Cancel")
}
```

#### `dropdown`
Dropdown menu component for toolbar and navigation use. Displays a toggleable menu of options.

**Properties:**
- `label`: String (required) - Button text for the dropdown
- `icon`: String - Optional icon name for the dropdown button

**Features:**
- **Animated transitions**: Smooth open/close animations
- **Icon support**: Optional icon before label text
- **Keyboard navigation**: Supports keyboard accessibility
- **Click outside to close**: Standard dropdown behavior

```
dropdown(label="Options", icon="cog") {
    menuitem(label="Settings", icon="sliders")
    menuitem(label="Help", icon="question-circle")
    menuitem(type=divider)
    menuitem(label="About", icon="info-circle")
}
```

#### `menuitem`
Individual item within a dropdown menu. Can be interactive, divider, or header.

**Properties:**
- `label`: String (required for normal/header types) - Display text
- `type`: Enum (`normal`, `divider`, `header`) - Item type (default: `normal`)
- `icon`: String - Icon name to display
- `shortcut`: String - Keyboard shortcut hint
- `transitionTo`: String - Screen ID for navigation
- `disabled`: Boolean - Disable the menu item

**Features:**
- **Multiple types**: Regular items, dividers, and section headers
- **Icon support**: Optional icon with automatic alignment
- **Keyboard shortcuts**: Display hints for keyboard commands
- **Navigation support**: Can trigger screen transitions
- **Disabled state**: Visual feedback for unavailable options

```
// Various menuitem types
menuitem(label="Save", icon="save", shortcut="Ctrl+S")
menuitem(type=divider)
menuitem(label="File Operations", type=header)
menuitem(label="Delete", icon="trash", disabled=true)
menuitem(label="Dashboard", icon="home", transitionTo="dashboard")
```

#### `expander`
Flexible spacer element that fills available space in toolbars and layouts.

**Features:**
- **Flex grow**: Automatically expands to fill available space
- **Toolbar spacing**: Perfect for pushing items to the right in toolbars
- **No properties needed**: Simple element with automatic behavior

```
// Toolbar with expander pushing items to the right
toolbar {
    text { Logo }
    expander  // Fills the space
    button(label="Login")
    button(label="Sign Up")
}
```

#### `row`
Simple row layout container that arranges children horizontally. Can be used outside of tables for general layout purposes.

**Features:**
- **Flex layout**: Uses flexbox for horizontal arrangement
- **Automatic spacing**: Provides gap between child elements
- **General purpose**: Can contain any elements unlike table rows

```
// Simple horizontal row layout
row {
    icon(iconName="home")
    text { Home }
}

// Row with multiple elements
row {
    text { Label: }
    field(placeholder="Enter value")
    button(label="Submit")
}
```

#### `grid`
Simple grid container element for basic grid layouts. This is a simpler alternative to `layout(type=grid)`.

**Features:**
- **Quick grid layouts**: No properties needed for basic grids
- **Automatic arrangement**: Children flow into grid cells
- **Responsive behavior**: Adapts to content and screen size

```
// Simple grid of cards
grid {
    card { text { Item 1 } }
    card { text { Item 2 } }
    card { text { Item 3 } }
    card { text { Item 4 } }
}
```

#### `theme`
Define custom CSS variables for dynamic styling and theme switching.

**Properties:**
- `name`: String - Theme identifier (required)
- `initial`: Boolean - Set as the initial active theme (optional, defaults to false)

**Features:**
- **CSS Variables**: Define any CSS custom property using `--variable-name: value` syntax
- **Dynamic Switching**: Switch themes using `transitionTo="theme:themeName"` on buttons/links
- **Smooth Transitions**: Automatic smooth transitions between themes
- **Multiple Themes**: Define multiple themes in the same document
- **Scoped to Preview**: Themes only affect the preview/presentation area, not the editor
- **Theme Isolation**: Mockups use their own isolated theme system independent of editor theme
- **Initial Theme**: Mark a theme with `initial=true` to auto-activate on load

```
// Define custom themes
theme(name=ocean, initial=true) {  // This theme will be active on load
    --primary-color: #0077be;
    --button-bg: #0077be;
    --button-hover-bg: #005a8b;
    --card-bg: #e6f3f9;
    --header-bg: #004466;
    --nav-bg: #005a8b;
}

theme(name=forest) {
    --primary-color: #228b22;
    --button-bg: #228b22;
    --button-hover-bg: #1a6b1a;
    --card-bg: #f0f8f0;
    --header-bg: #0d3d0d;
    --nav-bg: #1a6b1a;
}

// Use theme switching
screen {
    toolbar {
        button(label="Ocean Theme", transitionTo="theme:ocean")
        button(label="Forest Theme", transitionTo="theme:forest")
    }
}
```

### Non-Block Elements

Non-block elements are content elements that cannot contain other elements.

#### `button`
Interactive button element with optional icon support.

**Properties:**
- `style`: Enum (`primary`, `secondary`, `success`, `warning`, `danger`) - Button styling
- `label`: String - Button text content (empty string for icon-only buttons)
- `icon`: String - Icon name to display
- `transitionTo`: String - Screen ID for navigation

**Features:**
- **Icon support**: Can display icons with or without text
- **Icon-only buttons**: Use empty label with icon for compact buttons
- **Style variants**: Multiple color schemes for different actions

```
button(style=primary, label="Click Me")
button(label="Basic Button")
button(style=success, label="Save", transitionTo="dashboard")
button(icon="save", label="Save File")  // Icon with text
button(icon="menu", label="")  // Icon-only button
```

#### `field`
Form input field element with support for various input types and enhancements.

**Properties:**
- `label`: String (optional) - Field label text
- `value`: String - Current field value
- `placeholder`: String - Placeholder text
- `type`: Enum (`text`, `password`, `email`, `number`, `tel`, `dropdown`, `checkbox`, `radio`, `toggle`, `date`) - Input type
- `prefix`: String - Content before the input (text or "icon:iconname")
- `suffix`: String - Content after the input (text or "icon:iconname")
- `options`: String - Comma-separated options for dropdown/radio types
- `checked`: Boolean - For checkbox/toggle types
- `name`: String - Form field name for grouping radio buttons

**Features:**
- **Multiple input types**: Standard HTML5 inputs plus custom controls
- **Prefix/Suffix support**: Add text or icons before/after inputs
- **Custom controls**: Dropdown, checkbox, radio, and toggle switches
- **Accessible**: Proper labels and ARIA attributes

```
// Text input with icon prefix
field(label="Search", placeholder="Enter search term", prefix="icon:search")

// Field without label - just placeholder
field(placeholder="Search...")

// Email with suffix
field(label="Email", type=email, suffix="@company.com")

// Dropdown selection
field(label="Country", type=dropdown, options="USA,Canada,Mexico,Other", value="USA")

// Checkbox
field(label="I agree to the terms", type=checkbox, checked=true)

// Radio group
field(label="Payment Method", type=radio, options="Credit Card,PayPal,Bank Transfer", value="Credit Card")

// Toggle switch
field(label="Enable notifications", type=toggle, checked=false)

// Password with icon
field(label="Password", type=password, prefix="icon:lock", placeholder="Enter password")

// Date picker
field(label="Start Date", type=date, value="2024-01-01")
```

#### `link`
Clickable text link element. Can be used for external links, internal navigation, or plain text links.

**Properties:**
- `href`: String (optional) - URL for external links
- `transitionTo`: String (optional) - Screen ID for internal navigation
- `label`: String - Link text content

```
link(href="https://example.com", label="External Link")
link(transitionTo="home", label="Back to Home")
link(label="Plain Text Link")  // No href or transitionTo needed
```

#### `image`
Image display element with full media support including base64 data URIs.

**Properties:**
- `src`: String - Image source (HTTP/HTTPS URL only)
- `size`: Enum (`small`, `medium`, `large`, `full`) - Image size

**Body Content:**
- Base64 data URI can be placed in the body instead of src attribute

**Supported formats:**
- **HTTP/HTTPS URLs**: Use the `src` property for external images
- **Base64 Data URIs**: Place in the body content for embedded images
- **Placeholder text**: Shows styled placeholder for mockup purposes

```
// URL-based image
image(src="https://example.com/photo.jpg", size=large)

// Base64 image in body (foldable in editor)
image(size=medium) {
    data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA...
}

// Placeholder
image(src="Profile Picture Placeholder", size=small)
```

#### `icon`
Icon display element with comprehensive font icon support.

**Properties:**
- `fontSet`: Enum (`fontawesome`, `material`) - Icon font set (defaults to `fontawesome`)
- `iconName`: String - Icon name from the font set
- `style`: Enum (`primary`, `secondary`, `success`, `warning`, `danger`) - Icon color styling
- `size`: Enum (`small`, `medium`, `large`) - Icon size

**Supported Icon Sets:**
- **FontAwesome 6.x**: 60+ icons with automatic class mapping (fas, far, fab)
- **Material Icons**: 60+ Google Material Design icons
- **Emoji Fallbacks**: 40+ emoji alternatives for offline compatibility

**Popular Icons:**
- **UI**: home, user, settings, search, menu, close, check, edit, trash
- **Navigation**: arrow-left, arrow-right, chevron-up, chevron-down
- **Media**: play, pause, stop, volume-up, camera, image, video
- **Social**: facebook, twitter, github, linkedin, instagram, google
- **Business**: calendar, shopping-cart, credit-card, chart-bar, dollar-sign

```
icon(fontSet="fontawesome", iconName="home", style=primary, size=large)
icon(fontSet="material", iconName="settings", size=medium)
icon(iconName="star", style=warning) // FontAwesome star with warning color
```

#### `breadcrumb`
Breadcrumb navigation element showing the current page location.

**Properties:**
- `style`: String - Optional breadcrumb styling

**Features:**
- **Automatic separators**: Adds "/" between breadcrumb items
- **Semantic HTML**: Uses `<nav>` and `<ol>` for accessibility
- **Interactive links**: Supports `transitionTo` for navigation
- **Current page**: Last item appears as non-linked text

```
breadcrumb {
    link(transitionTo="home", label="Home")
    link(transitionTo="products", label="Products")
    text { Current Page }
}
```

### Navigation Elements

#### `tabs`
Container element for creating tabbed interfaces with multiple content panels.

**Properties:**
- `selectedIndex`: Number - Index of the initially selected tab (defaults to 0)

**Features:**
- **Interactive tabs**: Click to switch between content panels
- **Flexible content**: Tabs can contain nested elements or link to screens
- **Keyboard navigation**: Accessible tab switching
- **Smooth transitions**: Animated content changes
- **Screen navigation**: Tabs can navigate to different screens using `screenId`

```
tabs {
    tab(label="Overview") {
        text { Content for the overview tab }
        button(label="Learn More")
    }
    tab(label="Features") {
        grid {
            card { text { Feature 1 } }
            card { text { Feature 2 } }
        }
    }
    tab(label="Settings", screenId="settings-screen")
}
```

#### `tab`
Individual tab element within a tabs container.

**Properties:**
- `label`: String (required) - Tab button text
- `screenId`: String - Screen ID for navigation tabs (alternative to nested content)

**Features:**
- **Nested content**: Can contain any elements when used without `screenId`
- **Screen navigation**: Links to another screen when `screenId` is provided
- **Active state**: Visual indication of selected tab
- **Accessible**: Proper ARIA attributes for screen readers

```
// Tab with nested content
tab(label="Profile") {
    field(label="Name", value="John Doe")
    field(label="Email", type=email)
}

// Tab with screen navigation
tab(label="Dashboard", screenId="dashboard")
```

## Multi-Screen Navigation

MockupML supports interactive multi-screen applications with seamless navigation between screens.

**Features:**
- **Screen identification**: Each screen has a unique `id`
- **Initial screen**: Mark the starting screen with `initial=true`
- **Interactive navigation**: Use `transitionTo` on buttons and links
- **State management**: Automatic screen switching and state preservation
- **Visual indicators**: Navigation elements show interactive arrows (→)

**Navigation Properties:**
- `transitionTo`: String - Target screen ID for navigation
- `id`: String - Unique screen identifier
- `initial`: Boolean - Mark as starting screen

```
screen(id="login", initial=true, device=mobile) {
    header { text { Welcome Back } }
    content {
        field(label="Email", type=email)
        field(label="Password", type=password)
        button(transitionTo="dashboard", style=primary, label="Sign In")
        link(transitionTo="register", label="Create Account")
    }
}

screen(id="dashboard", device=mobile) {
    header {
        text { Dashboard }
        button(transitionTo="profile", label="Profile")
    }
    content {
        text { Welcome to your dashboard! }
        button(transitionTo="login", style=secondary, label="Sign Out")
    }
}

screen(id="register", device=mobile) {
    header {
        button(transitionTo="login", label="← Back")
        text { Create Account }
    }
    content {
        field(label="Full Name")
        field(label="Email", type=email)
        field(label="Password", type=password)
        button(transitionTo="dashboard", style=success, label="Create Account")
    }
}
```

**Complete E-commerce Flow Example:**
```
screen(id="home", initial=true) {
    nav {
        link(transitionTo="products", label="Products")
        button(transitionTo="cart", label="Cart (3)")
    }
    content {
        text { Welcome to our store! }
        button(transitionTo="products", label="Shop Now")
    }
}

screen(id="products") {
    header {
        breadcrumb {
            link(transitionTo="home", label="Home")
            text { Products }
        }
    }
    content {
        // Product listings with cart actions
        button(transitionTo="cart", label="Add to Cart")
    }
}
```

## File Organization

Large applications can be organized using include directives:

```
include(src="./components/header.mml")
include(src="./shared/navigation.mml")
```

## Usage Examples

### Basic Layout
```
screen(device=desktop, fontSize=14) {
    header {
        text { My Application }
        button(style=primary, label="Get Started")
    }
    
    content {
        text { Welcome to the application! }
        
        layout(type=grid, cols=2) {
            card {
                icon(fontSet="fontawesome", iconName="rocket", size=large)
                text { Feature 1 }
                text { Description here }
            }
            card {
                icon(fontSet="material", iconName="palette", size=large)
                text { Feature 2 }
                text { Another description }
            }
        }
    }
}
```

### Data Table
```
screen {
    content {
        table(style=striped) {
            row(type=header) {
                cell { text { Name } }
                cell { text { Status } }
                cell { text { Actions } }
            }
            row {
                cell { text { John Doe } }
                cell { text { Active } }
                cell {
                    button(style=primary, label="Edit")
                    button(style=danger, label="Delete")
                }
            }
        }
    }
}
```

### Form Layout
```
screen {
    panel {
        text { Contact Form }
        
        field(label="Name", placeholder="Enter your name", value="Bob Smith")
        field(label="Email", type=email, placeholder="user@example.com")
        field(label="Message", placeholder="Your message here")
        
        button(style=primary, label="Send Message")
    }
}
```

## Browser Usage

MockupML can be used in the browser with the built-in editor:

```bash
npm run dev
```

Open http://localhost:3000 to access the interactive editor with live preview.

## Node.js Usage

MockupML can be used programmatically in Node.js applications:

```javascript
const { parseMockupML } = require('mockupml');

const mockupML = `
screen(device=desktop, fontSize=16) {
    header {
        text(fontSize*=1.8) { Hello World }
    }
}
`;

const html = parseMockupML(mockupML, {
    baseFontSize: 16,
    deviceType: 'desktop'
});

console.log(html); // Generated HTML
```

## API Reference

### Core Parser Functions

- `parseMockupML(input, options)` - Parse MockupML and generate HTML
- `parseToAST(input, options)` - Parse MockupML and return AST
- `registerInterpreter(interpreter)` - Register custom element interpreter

### Parser Options

- `baseFontSize`: Number - Base font size for mathematical scaling (default: 14)
- `deviceType`: String - Target device type (default: 'desktop')
- `currentScreenId`: String - Active screen ID for multi-screen apps

## Advanced Theme Customization

MockupML provides an extensive CSS variable system for complete theme control. All mockup elements use `--mockup-*` prefixed variables that can be customized via the `theme` element.

### Available CSS Variables

**Core Colors:**
- `--mockup-primary-color`: Primary brand color
- `--mockup-primary-light`: Lighter variant of primary
- `--mockup-primary-bg`: Primary background color
- `--mockup-accent-color`: Accent color for highlights
- `--mockup-app-bg`: Application background
- `--mockup-surface-bg`: Surface/card backgrounds

**Text Colors:**
- `--mockup-text-primary`: Main text color
- `--mockup-text-secondary`: Secondary text
- `--mockup-text-muted`: Muted/disabled text
- `--mockup-text-on-primary`: Text on primary backgrounds

**Component Variables:**
- `--mockup-button-bg`: Button background
- `--mockup-button-text`: Button text color
- `--mockup-input-bg`: Input field background
- `--mockup-table-border`: Table borders
- `--mockup-link-color`: Link text color

**Spacing & Layout:**
- `--mockup-spacing-1` through `--mockup-spacing-16`: Consistent spacing scale
- `--mockup-border-radius`: Default border radius
- `--mockup-elevation-1` through `--mockup-elevation-24`: Shadow depths

### Example Custom Theme

```
theme(name=dark-mode) {
    --mockup-app-bg: #1a1a1a;
    --mockup-surface-bg: #2d2d2d;
    --mockup-text-primary: #ffffff;
    --mockup-text-secondary: #b0b0b0;
    --mockup-primary-color: #4a9eff;
    --mockup-button-bg: #4a9eff;
    --mockup-input-bg: #2d2d2d;
    --mockup-border-light: #404040;
}
```

## Extension and Customization

The modular parser architecture allows for easy extension with custom interpreters:

```javascript
import { BaseInterpreter } from './parser/interpreters/base-interpreter.js';

class CustomInterpreter extends BaseInterpreter {
    elementName = 'custom';
    
    parse(context) {
        // Custom parsing logic
        return {
            type: 'element',
            name: 'custom',
            properties: this.parseProperties(context),
            children: []
        };
    }
}

// Register the custom interpreter
registerInterpreter(new CustomInterpreter());
```

## Error Handling

MockupML provides detailed error messages with precise line and column information for quick debugging:

```
Error parsing MockupML: Unknown element 'BadElement' at line 5, column 12
```

## Deployment

### GitHub Pages

This project includes GitHub Actions for automatic deployment to GitHub Pages:

1. **Automatic Deployment**: Pushes to the `main` branch automatically trigger a deployment
2. **Manual Deployment**: Use the "Actions" tab to manually trigger deployment
3. **Build Process**: Runs `npm ci`, `npm run build`, and deploys the `dist/` directory

To set up GitHub Pages for your fork:

1. Go to your repository Settings → Pages
2. Set Source to "GitHub Actions"
3. Push to the `main` branch or manually trigger the workflow

The site will be available at: `https://your-username.github.io/your-repo-name`

### Local Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Build minified version for GitHub Pages
npm run build:docs
```

The `build:docs` command creates a minified, production-optimized version in the `docs/` directory suitable for GitHub Pages hosting.

---

*MockupML v1.0-alpha - A declarative markup language for rapid UI mockup generation*
